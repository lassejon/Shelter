# Shelter API - GitHub Copilot Instructions

## Tech Stack & Versions

Always use these specific versions:
- .NET 10.0
- C# 14
- Entity Framework Core 10.0
- ASP.NET Core 10.0
- PostgreSQL 18 with PostGIS 3.6

## Code Style Standards

### General Code Style
- Use primary constructors for all services and controllers
- Use collection expressions `[]` instead of `new List<T>()`
- Use file-scoped namespaces
- Use target-typed new where appropriate
- Use record types for DTOs

### Example:
```csharp
namespace Shelter.Api.Controllers;

public class ShelterService(ShelterDbContext context, ILogger<ShelterService> logger) : IShelterService
{
    private readonly List<int> numbers = [];  // ✅ Collection expression
    // NOT: private readonly List<int> numbers = new List<int>();  // ❌
}
```

---

## Clean Architecture Guidelines

### Project Structure
```
Shelter.Domain/         # Entities, Value Objects, Enums, Interfaces
Shelter.Application/    # Use Cases, Service Interfaces, DTOs, Validators
Shelter.Infrastructure/ # DbContext, Services Implementation, External APIs
Shelter.Api/           # Controllers, Middleware, Filters, Configuration
```

### Dependency Flow
```
Domain ← Application ← Infrastructure ← Api
```

- **Domain**: No dependencies on other layers (pure business logic)
- **Application**: References Domain only (interfaces, DTOs, business rules)
- **Infrastructure**: References Domain + Application (implements interfaces)
- **Api**: References all layers (composition root)

---

## 🚫 NO REPOSITORY PATTERN

**CRITICAL: We do NOT use the repository pattern. Services interact directly with DbContext.**

### Why?
- DbContext already implements Unit of Work pattern
- Repository pattern adds unnecessary abstraction
- Direct DbContext access provides more flexibility with `IQueryable<T>`
- Reduces boilerplate code
- EF Core is already a repository

### Example:
```csharp
// ✅ CORRECT - Direct DbContext usage
public class ShelterService(ShelterDbContext context, ILogger<ShelterService> logger)
{
    public async Task<List<Shelter>> GetNearbySheltersAsync(Point location, double radiusKm)
    {
        return await context.Shelters
            .Where(s => s.IsActive)
            .Where(s => s.Location.Distance(location) <= radiusKm * 1000)
            .Include(s => s.Pictures)
            .ToListAsync();
    }
}

// ❌ WRONG - Don't create repositories
public interface IShelterRepository { }
public class ShelterRepository : IShelterRepository { }
```

---

## Service Layer Pattern

### Interface in Application layer:
```csharp
// Shelter.Application/Services/IShelterService.cs
namespace Shelter.Application.Services;

public interface IShelterService
{
    Task<ShelterResponse> CreateAsync(CreateShelterRequest request, Guid ownerId);
    Task<ShelterResponse?> GetByIdAsync(Guid id);
    Task<List<ShelterResponse>> SearchAsync(ShelterSearchCriteria criteria);
    Task<bool> UpdateAsync(Guid id, UpdateShelterRequest request);
    Task<bool> DeleteAsync(Guid id);
}
```

### Implementation in Infrastructure layer:
```csharp
// Shelter.Infrastructure/Services/ShelterService.cs
namespace Shelter.Infrastructure.Services;

public class ShelterService(
    ShelterDbContext context,
    ILogger<ShelterService> logger) : IShelterService
{
    public async Task<ShelterResponse> CreateAsync(CreateShelterRequest request, Guid ownerId)
    {
        var shelter = new Domain.Shelters.Shelter
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = request.Name,
            Description = request.Description,
            Capacity = request.Capacity,
            Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
            BookingPolicy = request.BookingPolicy,
            IsActive = true
        };

        context.Shelters.Add(shelter);
        await context.SaveChangesAsync();

        logger.LogInformation("Created shelter {ShelterId} for owner {OwnerId}", shelter.Id, ownerId);

        return MapToResponse(shelter);
    }
}
```

---

## Entity Configuration

Keep entities in Domain layer clean - no data annotations for database mapping.

### Domain Entity (Clean):
```csharp
// Shelter.Domain/Shelters/Shelter.cs
namespace Shelter.Domain.Shelters;

public class Shelter
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public Point Location { get; set; } = null!;
    public BookingPolicy BookingPolicy { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Picture> Pictures { get; set; } = [];
}
```

### Entity Configuration (Fluent API in Infrastructure):
```csharp
// Shelter.Infrastructure/Persistence/Configurations/ShelterConfiguration.cs
namespace Shelter.Infrastructure.Persistence.Configurations;

public class ShelterConfiguration : IEntityTypeConfiguration<Domain.Shelters.Shelter>
{
    public void Configure(EntityTypeBuilder<Domain.Shelters.Shelter> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.Location)
            .IsRequired()
            .HasColumnType("geometry (point)");

        builder.HasOne(s => s.Owner)
            .WithMany(u => u.OwnedShelters)
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## DTOs and Mapping

### DTO Naming Conventions:
- **API Requests/Responses**: Use `-Request` and `-Response` suffixes
- **Internal DTOs**: Use `-Dto` suffix only for internal transfers (not going in/out of API)
- **Organize in folders**: Separate `Requests/` and `Responses/` folders

### Use record types for DTOs:
```csharp
// Shelter.Application/Requests/CreateShelterRequest.cs
namespace Shelter.Application.Requests;

public record CreateShelterRequest(
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    BookingPolicy BookingPolicy);

// Shelter.Application/Responses/ShelterResponse.cs
namespace Shelter.Application.Responses;

public record ShelterResponse(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    bool IsActive);
```

### Folder Structure:
```
Shelter.Application/
├── Requests/
│   ├── CreateShelterRequest.cs
│   ├── UpdateShelterRequest.cs
│   └── SearchShelterRequest.cs
├── Responses/
│   ├── ShelterResponse.cs
│   ├── BookingResponse.cs
│   └── SearchResultsResponse.cs
└── DTOs/  (Internal use only)
    └── ShelterDto.cs
```

### Domain/Application Model Mapping

**CRITICAL: Keep services clean by putting mapping logic in Request/Response objects.**

#### Request Objects - `ToDomain()` Method
Request objects should have a `ToDomain()` method that converts the request to a domain entity:

```csharp
// Shelter.Application/Requests/CreateShelterRequest.cs
public record CreateShelterRequest(
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    ShelterBookingPolicy BookingPolicy,
    List<PictureRequest>? Pictures)
{
    public Shelter ToDomain(Guid ownerId)
    {
        var shelter = new Shelter
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = Name,
            Description = Description,
            Capacity = Capacity,
            Location = new Point(Longitude, Latitude) { SRID = 4326 },
            BookingPolicy = BookingPolicy,
            IsActive = true,
            Pictures = []
        };

        if (Pictures != null && Pictures.Count > 0)
        {
            foreach (var pictureRequest in Pictures)
            {
                shelter.Pictures.Add(pictureRequest.ToDomain(ownerId, shelter.Id));
            }
        }

        return shelter;
    }
}
```

#### Update Requests - `ApplyTo()` Method
Update requests should have an `ApplyTo()` method that applies changes to an existing entity:

```csharp
// Shelter.Application/Requests/UpdateShelterRequest.cs
public record UpdateShelterRequest(
    string Name,
    string? Description,
    int Capacity,
    bool IsActive)
{
    public void ApplyTo(Shelter shelter)
    {
        shelter.Name = Name;
        shelter.Description = Description;
        shelter.Capacity = Capacity;
        shelter.IsActive = IsActive;
    }
}
```

#### Response Objects - `FromDomain()` Static Method
Response objects should have a static `FromDomain()` method that creates a response from a domain entity:

```csharp
// Shelter.Application/Responses/ShelterResponse.cs
public record ShelterResponse(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    ShelterBookingPolicy BookingPolicy,
    bool IsActive,
    List<PictureResponse> Pictures)
{
    public static ShelterResponse FromDomain(Shelter shelter) => new(
        shelter.Id,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Location.Y, // Latitude
        shelter.Location.X, // Longitude
        shelter.BookingPolicy,
        shelter.IsActive,
        shelter.Pictures
            .OrderBy(p => p.SortOrder)
            .Select(PictureResponse.FromDomain)
            .ToList());
}

public record PictureResponse(
    Guid Id,
    string Url,
    string? Caption,
    int SortOrder)
{
    public static PictureResponse FromDomain(Picture picture) => new(
        picture.Id,
        picture.Url,
        picture.Caption,
        picture.SortOrder);
}
```

#### Clean Service Code
With mapping in Request/Response objects, services stay clean and focused:

```csharp
// ✅ CORRECT - Clean service with no mapping clutter
public class ShelterService(ShelterDbContext context, ILogger<ShelterService> logger) : IShelterService
{
    public async Task<ShelterResponse> CreateAsync(CreateShelterRequest request, Guid ownerId)
    {
        var shelter = request.ToDomain(ownerId);
        
        context.Shelters.Add(shelter);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Created shelter {ShelterId}", shelter.Id);
        
        return ShelterResponse.FromDomain(shelter);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateShelterRequest request)
    {
        var shelter = await context.Shelters.FindAsync(id);
        
        if (shelter == null)
        {
            return false;
        }
        
        request.ApplyTo(shelter);
        
        await context.SaveChangesAsync();
        
        return true;
    }
}

// ❌ WRONG - Mapping logic cluttering the service
public class ShelterService(ShelterDbContext context) : IShelterService
{
    public async Task<ShelterResponse> CreateAsync(CreateShelterRequest request, Guid ownerId)
    {
        // Don't do this - mapping should be in the request object!
        var shelter = new Shelter
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            // ... many more lines of mapping
        };
        
        context.Shelters.Add(shelter);
        await context.SaveChangesAsync();
        
        // Don't do this - mapping should be in the response object!
        return new ShelterResponse(
            shelter.Id,
            shelter.Name,
            shelter.Description,
            // ... many more lines of mapping
        );
    }
}
```

---

## Transaction Management

### Default Behavior - No Explicit Transactions Needed:
EF Core automatically wraps `SaveChangesAsync()` in a transaction. For most operations, **explicit transactions are unnecessary**.

```csharp
// ✅ CORRECT - Single SaveChanges (implicit transaction)
public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, Guid bookerId)
{
    var booking = new Booking { /* ... */ };
    context.Bookings.Add(booking);

    context.Notifications.Add(new Notification
    {
        Booking = booking,
        Message = "Booking confirmed"
    });

    await context.SaveChangesAsync(); // Everything commits or rolls back together

    return MapToResponse(booking);
}
```

### When to Use Explicit Transactions:
Only use explicit transactions when you need **multiple SaveChangesAsync calls** or **specific isolation levels**:

```csharp
// ✅ Multiple SaveChanges operations that must be atomic
public async Task<TransferResult> TransferBookingAsync(Guid bookingId, Guid newShelterId)
{
    await using var transaction = await context.Database.BeginTransactionAsync();

    var booking = await context.Bookings.FindAsync(bookingId);
    booking.ShelterId = newShelterId;
    await context.SaveChangesAsync();

    context.AuditLogs.Add(new AuditLog { /* ... */ });
    await context.SaveChangesAsync();

    await transaction.CommitAsync();
    return new TransferResult(Success: true);
}
```

---

## Error Handling

### Let Exceptions Bubble - Don't Use Empty Try-Catch Blocks

❌ **ANTI-PATTERN** - Don't do this:
```csharp
// ❌ WRONG - Pointless try-catch that just rethrows
public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request)
{
    try
    {
        var booking = new Booking { /* ... */ };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return MapToResponse(booking);
    }
    catch
    {
        throw; // Just let it bubble naturally!
    }
}
```

✅ **CORRECT** - Services throw domain exceptions and let them bubble:
```csharp
public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, Guid bookerId)
{
    var shelter = await context.Shelters.FindAsync(request.ShelterId)
        ?? throw new NotFoundException($"Shelter {request.ShelterId} not found");

    if (!shelter.IsActive)
    {
        throw new BusinessRuleException("Cannot book inactive shelter");
    }

    var hasConflict = await context.Bookings
        .AnyAsync(b => b.ShelterId == request.ShelterId
            && b.Status != BookingStatus.Cancelled
            && b.StartUtc < request.EndUtc
            && b.EndUtc > request.StartUtc);

    if (hasConflict)
    {
        throw new BookingConflictException("Shelter is already booked for this period");
    }

    var booking = new Booking
    {
        Id = Guid.NewGuid(),
        ShelterId = request.ShelterId,
        BookerId = bookerId,
        StartUtc = request.StartUtc,
        EndUtc = request.EndUtc,
        Status = BookingStatus.Confirmed
    };

    context.Bookings.Add(booking);
    await context.SaveChangesAsync();

    return MapToResponse(booking);
}
```

### When Try-Catch IS Appropriate:
```csharp
// ✅ Adding context to exceptions
public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, Guid bookerId)
{
    try
    {
        // ... booking logic
        await context.SaveChangesAsync();
    }
    catch (DbUpdateException ex)
    {
        logger.LogError(ex, "Failed to create booking for user {UserId}", bookerId);
        throw new BookingCreationException("Unable to create booking. Please try again.", ex);
    }
}

// ✅ Transforming exceptions to domain exceptions
public async Task ProcessPaymentAsync(Guid bookingId, PaymentRequest payment)
{
    try
    {
        await paymentGateway.ChargeAsync(payment);
    }
    catch (PaymentGatewayException ex)
    {
        throw new PaymentFailedException("Payment processing failed", ex);
    }
}
```

---

## API Development Guidelines (Zalando REST API Guidelines)

### Reference
Follow the Zalando RESTful API Guidelines: https://opensource.zalando.com/restful-api-guidelines/

### Controller-Based Architecture
**We use Controllers, NOT Minimal APIs.**

### Controller Structure:
```csharp
namespace Shelter.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SheltersController(
    IShelterService shelterService,
    ILogger<SheltersController> logger) : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShelterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShelterDto>> GetById(Guid id)
    {
        var shelter = await shelterService.GetByIdAsync(id);

        if (shelter == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Shelter not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Shelter with ID {id} does not exist"
            });
        }

        return Ok(shelter);
    }
}
```

### URL Design (Zalando):
- Use lowercase, hyphen-separated path segments: `/api/v1/booking-requests`
- Use plural for collections: `/shelters`, `/bookings`
- Use path parameters for resource IDs: `/shelters/{shelter-id}`
- Use query parameters for filtering: `/shelters?latitude=55.6761&longitude=12.5683&radius=10`

**Examples:**
```
GET    /api/v1/shelters
GET    /api/v1/shelters/{id}
POST   /api/v1/shelters
PUT    /api/v1/shelters/{id}
DELETE /api/v1/shelters/{id}

GET    /api/v1/shelters/{id}/bookings
POST   /api/v1/shelters/{id}/bookings
GET    /api/v1/bookings/{id}
```

### HTTP Methods (Zalando):
- `GET` - Retrieve resource(s) (safe, idempotent, cacheable)
- `POST` - Create new resource (not idempotent)
- `PUT` - Replace entire resource (idempotent)
- `PATCH` - Partial update (use sparingly)
- `DELETE` - Remove resource (idempotent)

### Response Status Codes (Zalando):

**Success:**
- `200 OK` - Successful GET, PUT, PATCH, DELETE with body
- `201 Created` - Successful POST that creates a resource
- `202 Accepted` - Request accepted for async processing
- `204 No Content` - Successful request with no response body

**Client Errors:**
- `400 Bad Request` - Invalid request format/validation failure
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Authenticated but not authorized
- `404 Not Found` - Resource doesn't exist
- `409 Conflict` - Request conflicts with current state
- `422 Unprocessable Entity` - Semantic errors in request

**Server Errors:**
- `500 Internal Server Error` - Unexpected server error
- `503 Service Unavailable` - Service temporarily unavailable

### Response Structure:

**Success Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Copenhagen Central Shelter",
  "description": "Large shelter in city center",
  "capacity": 50,
  "latitude": 55.6761,
  "longitude": 12.5683,
  "is_active": true
}
```

**Error Response (RFC 7807 Problem Details):**
```json
{
  "type": "https://api.shelter.com/problems/validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "The capacity must be greater than 0",
  "instance": "/api/v1/shelters",
  "errors": {
    "capacity": ["Must be greater than 0"]
  }
}
```

### Pagination (Zalando):
Use cursor-based or offset-based pagination for collections.

**Query Parameters:**
- `limit` - Number of items per page (default: 20, max: 100)
- `cursor` - Opaque cursor for next page (preferred)
- `offset` - Offset for page (alternative)

**Response with pagination:**
```json
{
  "items": [...],
  "pagination": {
    "cursor": "eyJpZCI6IjEyMzQ1Njc4In0=",
    "limit": 20,
    "has_more": true
  }
}
```

### Validation:
Use FluentValidation (preferred):
```csharp
public class CreateShelterDtoValidator : AbstractValidator<CreateShelterDto>
{
    public CreateShelterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 1000);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);
    }
}
```

### Authorization:
```csharp
[HttpPost]
[Authorize] // Requires authentication
public async Task<ActionResult<ShelterDto>> Create([FromBody] CreateShelterDto dto)
{
    var userId = User.GetUserId();
    // ...
}

[HttpDelete("{id}")]
[Authorize(Policy = "AdminOnly")] // Requires specific policy
public async Task<IActionResult> Delete(Guid id)
{
    // ...
}
```

### Async/Await:
**ALL database operations must be async:**
```csharp
// ✅ CORRECT
[HttpGet("{id}")]
public async Task<ActionResult<ShelterDto>> GetById(Guid id)
{
    var shelter = await shelterService.GetByIdAsync(id);
    return shelter == null ? NotFound() : Ok(shelter);
}

// ❌ WRONG
[HttpGet("{id}")]
public ActionResult<ShelterDto> GetById(Guid id)
{
    var shelter = shelterService.GetByIdAsync(id).Result; // BAD!
    return shelter == null ? NotFound() : Ok(shelter);
}
```

---

## Controller Checklist

Every controller endpoint should have:

1. ✅ Primary constructor with dependencies
2. ✅ `[ApiController]` attribute on class
3. ✅ `[Route]` attribute defining URL pattern
4. ✅ HTTP method attributes (`[HttpGet]`, `[HttpPost]`, etc.)
5. ✅ `[ProducesResponseType]` for all possible responses
6. ✅ `[Authorize]` where authentication is required
7. ✅ Async methods with `Task<ActionResult<T>>` return type
8. ✅ Proper status codes in responses
9. ✅ Logging for important operations
10. ✅ Input validation (FluentValidation preferred)

---

## Anti-Patterns to Avoid

❌ Don't create repository interfaces/classes
❌ Don't expose entities directly - always use DTOs
❌ Don't put business logic in controllers - use services
❌ Don't use `Result` or `.Wait()` on async operations
❌ Don't return 200 for errors - use appropriate status codes
❌ Don't use Minimal APIs - use Controllers
❌ Don't use empty try-catch-rethrow blocks
❌ Don't use data annotations on Domain entities
❌ Don't use explicit transactions unless needed
❌ Don't create God controllers - keep them focused

---

## Key Principles Summary

1. ✅ Services use DbContext directly (no repositories)
2. ✅ Interfaces in Application, implementations in Infrastructure
3. ✅ Primary constructors for dependency injection
4. ✅ Record types for DTOs with `-Request`/`-Response` suffixes
5. ✅ Fluent API for entity configuration
6. ✅ Keep Domain layer dependency-free
7. ✅ Use IQueryable for flexible queries
8. ✅ Business logic in services, not controllers
9. ✅ Let DbContext manage transactions (explicit only when needed)
10. ✅ Let exceptions bubble, handle globally in middleware
11. ✅ No try-catch-rethrow anti-patterns
12. ✅ Try-catch only when adding value (logging, transformation)
13. ✅ Follow Zalando REST API Guidelines
14. ✅ Use Controllers, not Minimal APIs
15. ✅ All database operations must be async
