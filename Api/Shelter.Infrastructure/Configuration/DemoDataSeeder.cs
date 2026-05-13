using App.Common;
using App.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shelter.Domain.Assets;
using Shelter.Domain.Auth;
using Shelter.Domain.Reviews;
using Shelter.Domain.Shelters;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Configuration;

/// <summary>
/// One-shot demo data seeder. Creates two pieces of data on first run:
/// <list type="number">
///   <item><c>demo@shelter.local</c> owning 2000 random shelters spread across Denmark.</item>
///   <item><c>lasss@live.dk</c> owning the Hummingen demo-shelter, plus 10 reviewers who each
///     leave a review (5 with attached photos).</item>
/// </list>
/// Registered conditionally from <c>AddInfrastructure</c> when the <c>Demo:SeedEnabled</c> flag
/// is set, runs once at host startup, idempotent (skips if <c>demo@shelter.local</c> already
/// exists). Photos are best-effort fetched from picsum.photos and shelterbyg.dk; on failure
/// (offline, blocked), shelters seed without pictures.
/// </summary>
internal sealed class DemoDataSeeder(
    IServiceProvider services,
    ILogger<DemoDataSeeder> logger) : IHostedService
{
    private const string DemoEmail = "demo@shelter.local";
    private const string DemoPassword = "Demo123!";
    private const int ShelterCount = 2000;
    private const int SeedPhotoCount = 5;
    private const int RandomSeed = 12345;

    // -- Hummingen demo --
    private const string HummingenOwnerEmail = "lasss@live.dk";
    private const string HummingenOwnerFirstName = "Lasse";
    private const string HummingenOwnerLastName = "Frederiksen";
    private const string DemoUserPassword = "Passw0rd*";

    private const string HummingenName = "Hummingen shelter";
    private const string HummingenDescription =
        "Super lækkert shelter med bålplads og tæt på stranden";
    private const double HummingenLat = 54.714185;
    private const double HummingenLng = 11.213814;
    private const int HummingenCapacity = 4;

    private static readonly string[] HummingenPhotoUrls =
    {
        "https://shelterbyg.dk/cdn/shop/files/shelter_type16_angle_front.jpg?v=1759825279",
        "https://shelterbyg.dk/cdn/shop/files/shelter_type16_interior_ovenlys.jpg?v=1759825279",
        "https://shelterbyg.dk/cdn/shop/files/shelter_type16_front.jpg?v=1759825279",
        "https://shelterbyg.dk/cdn/shop/files/Shelter_type16_back.jpg?v=1759825279",
        "https://shelterbyg.dk/cdn/shop/files/shelter_type16_interior_bord.jpg?v=1759825279",
        "https://shelterbyg.dk/cdn/shop/files/shelter_type16_tag_ovenlys.jpg?v=1759825279",
    };

    private static readonly (string Email, string FirstName, string LastName, Rating Rating, string Comment, bool WithPhotos)[] HummingenReviewers =
    {
        ("lasssjon@gmail.com",             "Lasse",    "Jon",          Rating.Excellent, "Familie-tur til Lolland og det her shelter var helt perfekt. Bålpladsen lige til en lang aften, og stranden er kun et stenkast væk. Lasse var en superflink vært.",  true),
        ("mette.andersen@shelter.local",   "Mette",    "Andersen",     Rating.Excellent, "Fantastisk shelter! Bålpladsen var perfekt og stranden ligger kun 200 meter væk. Børnene elskede stedet.",                                                          true),
        ("soren.nielsen@shelter.local",    "Søren",    "Nielsen",      Rating.Excellent, "Skønt sted med god plads. Rent, hyggeligt og veludstyret. Vi kommer igen næste sommer.",                                                                              true),
        ("kristine.hansen@shelter.local",  "Kristine", "Hansen",       Rating.Excellent, "Lige hvad vi havde drømt om til en weekend-tur. Brænde til bålet var inkluderet — stor bonus.",                                                                       true),
        ("magnus.larsen@shelter.local",    "Magnus",   "Larsen",       Rating.Excellent, "Helt klar 5 stjerner. Rolig nat under stjernerne og super tæt på vandet. Anbefales varmt.",                                                                           true),
        ("ida.pedersen@shelter.local",     "Ida",      "Pedersen",     Rating.Good,      "God plads og fin stand. Stien til stranden er let at finde. Lidt myg om aftenen, men det er jo Lolland-sommer.",                                                       false),
        ("jens.christensen@shelter.local", "Jens",     "Christensen",  Rating.Excellent, "Hyggeligt og afsides. Vi nød solnedgangen fra bænken ved bålpladsen.",                                                                                                 false),
        ("anne.jorgensen@shelter.local",   "Anne",     "Jørgensen",    Rating.Fair,      "Pænt sted, men kunne godt bruge en frisk gennemgang. Stadig hyggeligt for en enkelt nat.",                                                                             false),
        ("thomas.rasmussen@shelter.local", "Thomas",   "Rasmussen",    Rating.Good,      "Perfekt udgangspunkt for en cykeltur langs vestkysten af Lolland. Anbefales.",                                                                                         false),
        ("emma.sorensen@shelter.local",    "Emma",     "Sørensen",     Rating.Excellent, "Charmerende lille shelter med en helt særlig ro. Vi kommer igen til efteråret.",                                                                                      false),
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<User>>();
        var existing = await userManager.FindByEmailAsync(DemoEmail);
        if (existing is not null)
        {
            logger.LogInformation(
                "Demo user {Email} already exists. Skipping demo seed (idempotent).",
                DemoEmail);
            return;
        }

        var db = sp.GetRequiredService<Persistence.ShelterDbContext>();
        var storage = sp.GetRequiredService<IFileStorage>();
        var clock = sp.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var random = new Random(RandomSeed);

        // 1. demo@shelter.local owns 2000 randomly-distributed shelters
        var demoUser = await CreateUserAsync(
            userManager, DemoEmail, "Demo", "Owner", DemoPassword, AppRoles.ShelterOwner);

        var picsumAssets = await TryUploadPicsumPhotosAsync(
            demoUser.Id, storage, now, cancellationToken);

        var randomShelters = BuildRandomShelters(demoUser.Id, picsumAssets, random, now);

        // 2. Hummingen demo: dedicated owner + 10 reviewers (5 with photos)
        var (hummingen, hummingenAssets, hummingenReviews) = await SeedHummingenAsync(
            userManager, storage, picsumAssets, random, now, cancellationToken);

        // 3. Single save commits all shelters, assets, pictures, reviews and review pictures
        if (picsumAssets.Count > 0)
        {
            db.Assets.AddRange(picsumAssets);
        }
        if (hummingenAssets.Count > 0)
        {
            db.Assets.AddRange(hummingenAssets);
        }
        db.Shelters.AddRange(randomShelters);
        db.Shelters.Add(hummingen);
        db.Reviews.AddRange(hummingenReviews);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo seed complete: {DemoEmail} owns {RandomCount} random shelters, "
            + "{OwnerEmail} owns Hummingen with {ReviewCount} reviews "
            + "({PicsumCount} picsum photos, {HummingenCount} shelterbyg photos).",
            DemoEmail, ShelterCount, HummingenOwnerEmail, hummingenReviews.Count,
            picsumAssets.Count, hummingenAssets.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // -- 2000 random shelters --------------------------------------------------------------------

    private List<ShelterEntity> BuildRandomShelters(
        Guid ownerId,
        List<Asset> photoAssets,
        Random random,
        DateTimeOffset now)
    {
        var shelters = new List<ShelterEntity>(ShelterCount);

        for (var i = 0; i < ShelterCount; i++)
        {
            var (latitude, longitude) = PickLocation(random);
            var shelter = ShelterEntity.Create(
                ownerId: ownerId,
                name: PickName(random, i),
                description: PickDescription(random),
                capacity: random.Next(2, 16),
                latitude: latitude,
                longitude: longitude,
                bookingPolicy: PickEnum<ShelterBookingPolicy>(random),
                bookingApprovalMode: PickEnum<BookingApprovalMode>(random),
                now: now);

            if (photoAssets.Count > 0)
            {
                var photoCount = random.Next(0, 3);
                for (var p = 0; p < photoCount; p++)
                {
                    var asset = photoAssets[random.Next(photoAssets.Count)];
                    shelter.AddPicture(asset.Id, caption: null, now);
                }
            }

            shelters.Add(shelter);
        }

        return shelters;
    }

    // -- Hummingen + 10 reviewers ----------------------------------------------------------------

    private async Task<(ShelterEntity Shelter, List<Asset> Assets, List<Review> Reviews)>
        SeedHummingenAsync(
            UserManager<User> userManager,
            IFileStorage storage,
            List<Asset> picsumAssets,
            Random random,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        // a) Owner — lasss@live.dk with ShelterOwner role
        var owner = await CreateUserAsync(
            userManager,
            HummingenOwnerEmail,
            HummingenOwnerFirstName,
            HummingenOwnerLastName,
            DemoUserPassword,
            AppRoles.ShelterOwner);

        // b) Shelter-specific photos (fetched from shelterbyg.dk on first run)
        var assets = await TryUploadFromUrlsAsync(
            owner.Id, HummingenPhotoUrls, "seed/hummingen", storage, now, cancellationToken);

        // c) The shelter itself — Both/RequiresApproval so the owner can demo the approve-booking flow
        var hummingen = ShelterEntity.Create(
            ownerId: owner.Id,
            name: HummingenName,
            description: HummingenDescription,
            capacity: HummingenCapacity,
            latitude: HummingenLat,
            longitude: HummingenLng,
            bookingPolicy: ShelterBookingPolicy.Both,
            bookingApprovalMode: BookingApprovalMode.RequiresApproval,
            now: now);

        foreach (var asset in assets)
        {
            hummingen.AddPicture(asset.Id, caption: null, now);
        }

        // d) 10 reviewers + reviews (first 5 attach picsum photos)
        var reviews = new List<Review>(HummingenReviewers.Length);
        foreach (var spec in HummingenReviewers)
        {
            var reviewer = await CreateUserAsync(
                userManager, spec.Email, spec.FirstName, spec.LastName, DemoUserPassword);

            var review = Review.Create(
                shelterId: hummingen.Id,
                reviewerId: reviewer.Id,
                rating: spec.Rating,
                comment: spec.Comment,
                now: now);

            if (spec.WithPhotos && picsumAssets.Count > 0)
            {
                var photoCount = random.Next(1, 3); // 1 or 2 photos per review
                for (var p = 0; p < photoCount; p++)
                {
                    var asset = picsumAssets[random.Next(picsumAssets.Count)];
                    review.AddPicture(asset.Id, caption: null, now);
                }
            }

            reviews.Add(review);
        }

        logger.LogInformation(
            "Created Hummingen shelter owned by {Owner} with {ReviewCount} reviews.",
            HummingenOwnerEmail, reviews.Count);

        return (hummingen, assets, reviews);
    }

    // -- User helpers ----------------------------------------------------------------------------

    private async Task<User> CreateUserAsync(
        UserManager<User> userManager,
        string email,
        string firstName,
        string lastName,
        string password,
        string? role = null)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            var errors = string.Join("; ", create.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Failed to create user {email}: {errors}");
        }

        if (role is not null)
        {
            var addRole = await userManager.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                var errors = string.Join("; ", addRole.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Failed to add role {role} to user {email}: {errors}");
            }
        }

        logger.LogInformation(
            "Created user {Email}{Role}.",
            email, role is null ? string.Empty : $" (role: {role})");
        return user;
    }

    // -- Photo upload helpers --------------------------------------------------------------------

    private async Task<List<Asset>> TryUploadPicsumPhotosAsync(
        Guid uploadedById,
        IFileStorage storage,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var urls = Enumerable.Range(1, SeedPhotoCount)
            .Select(i => $"https://picsum.photos/seed/shelter-{i}/800/600")
            .ToArray();

        return await TryUploadFromUrlsAsync(
            uploadedById, urls, "seed", storage, now, cancellationToken);
    }

    private async Task<List<Asset>> TryUploadFromUrlsAsync(
        Guid uploadedById,
        IReadOnlyList<string> urls,
        string blobKeyPrefix,
        IFileStorage storage,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assets = new List<Asset>(urls.Count);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        for (var i = 0; i < urls.Count; i++)
        {
            try
            {
                var bytes = await http.GetByteArrayAsync(urls[i], cancellationToken);
                var blobKey = $"{blobKeyPrefix}/photo-{i + 1}.jpg";

                using var stream = new MemoryStream(bytes);
                await storage.UploadAsync(blobKey, stream, "image/jpeg", cancellationToken);

                assets.Add(Asset.Create(uploadedById, blobKey, "image/jpeg", now));
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to fetch/upload photo from {Url}. Continuing without it.",
                    urls[i]);
            }
        }

        if (assets.Count == 0)
        {
            logger.LogWarning(
                "No photos uploaded for prefix {Prefix}. Shelters/reviews under it will have no pictures.",
                blobKeyPrefix);
        }
        else
        {
            logger.LogInformation(
                "Uploaded {Count} photos to blob storage under {Prefix}/.",
                assets.Count, blobKeyPrefix);
        }

        return assets;
    }

    // -- Random data -----------------------------------------------------------------------------

    private static readonly (string City, double Lat, double Lng)[] Anchors =
    {
        ("København",      55.6761, 12.5683),
        ("Aarhus",         56.1629, 10.2039),
        ("Odense",         55.4038, 10.4024),
        ("Aalborg",        57.0488,  9.9217),
        ("Esbjerg",        55.4760,  8.4594),
        ("Randers",        56.4607, 10.0364),
        ("Kolding",        55.4915,  9.4720),
        ("Horsens",        55.8607,  9.8503),
        ("Vejle",          55.7090,  9.5354),
        ("Roskilde",       55.6415, 12.0803),
        ("Herning",        56.1399,  8.9712),
        ("Helsingør",      56.0364, 12.6135),
        ("Silkeborg",      56.1697,  9.5510),
        ("Næstved",        55.2300, 11.7600),
        ("Frederikshavn",  57.4407, 10.5462),
        ("Rønne",          55.1011, 14.7044),
        ("Nykøbing Mors",  56.7929,  8.8530),
        ("Søllested",      54.7906, 11.3056),
        ("Kalundborg",     55.6817, 11.0922),
        ("Aabenraa",       55.0436,  9.4174),
        ("Stege",          54.9839, 12.2861),
        ("Grenaa",         56.4106, 10.8757),
    };

    private static (double lat, double lng) PickLocation(Random random)
    {
        var (_, anchorLat, anchorLng) = Anchors[random.Next(Anchors.Length)];
        var lat = anchorLat + (random.NextDouble() - 0.5) * 0.5;
        var lng = anchorLng + (random.NextDouble() - 0.5) * 0.8;
        lat = Math.Clamp(lat, 54.5, 57.8);
        lng = Math.Clamp(lng, 8.0, 15.2);
        return (lat, lng);
    }

    private static readonly string[] NameStems =
    {
        "Skovly", "Bækhytten", "Birkely", "Granly", "Egely", "Bøgely",
        "Lyngshelter", "Klithytten", "Strandbo", "Søfryd", "Mosegård",
        "Stenbjerg", "Egeskoven", "Pilehuset", "Tornbygård", "Hedely",
        "Marehalm", "Vildmosen", "Fjeldstien", "Tørveskoven", "Skjulestedet",
        "Bækken", "Sølyst", "Skovskellet", "Krattet", "Mosen", "Lyngmosen",
        "Egekrogen", "Bøgekrogen", "Birkekrogen",
    };

    private static string PickName(Random random, int index)
    {
        var stem = NameStems[random.Next(NameStems.Length)];
        var city = Anchors[random.Next(Anchors.Length)].City;
        return $"{stem} ved {city} #{index + 1}";
    }

    private static readonly string[] DescriptionTemplates =
    {
        "Hyggelig shelter med udsigt til {feature}. Plads til hele familien.",
        "Naturskønt sted tæt på {feature}. Perfekt til en weekendtur.",
        "Roligt skovareal med {feature} lige uden for døren.",
        "Idyllisk beliggenhed nær {feature}. Bål-plads og brænde inkluderet.",
        "Afsides shelter med {feature} i baghaven. Stjernehimlen er gratis.",
        "Charmerende træhytte ved {feature}. God til både par og venner.",
        "Solrigt sted med kort gåafstand til {feature}.",
        "Centralt placeret med {feature} kun et stenkast væk.",
    };

    private static readonly string[] Features =
    {
        "søen", "skoven", "havet", "marken", "kratet", "engen", "fjorden",
        "bækken", "lyngheden", "klitterne", "mosen", "højderne",
    };

    private static string PickDescription(Random random)
    {
        var template = DescriptionTemplates[random.Next(DescriptionTemplates.Length)];
        var feature = Features[random.Next(Features.Length)];
        return template.Replace("{feature}", feature);
    }

    private static T PickEnum<T>(Random random) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return values[random.Next(values.Length)];
    }
}
