# Clean Architecture Guidelines for Shelter API

## Project Structure

```
Shelter.Domain/         # Entities, Value Objects, Enums, Interfaces
Shelter.Application/    # Use Cases, Service Interfaces, DTOs, Validators
Shelter.Infrastructure/ # DbContext, Services Implementation, External APIs
Shelter.Api/           # Controllers, Middleware, Filters, Configuration
```

## Core Principles

### 1. NO Repository Pattern
We do NOT use the repository pattern. Services interact directly with DbContext.

**Why?**
- DbContext already implements Unit of Work pattern
- Repository pattern adds unnecessary abstraction
- Direct DbContext access provides more flexibility with `IQueryable<T>`
- Reduces boilerplate code
- EF Core is already a repository

**Example:**
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

### 2. Service Layer Pattern

**Interface in Application layer:**
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

**Implementation in Infrastructure layer:**
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
    
    public async Task<ShelterResponse?> GetByIdAsync(Guid id)
    {
        var shelter = await context.Shelters
            .Include(s => s.Pictures)
            .Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        return shelter == null ? null : MapToResponse(shelter);
    }
    
    private static ShelterResponse MapToResponse(Domain.Shelters.Shelter shelter) => new(
        shelter.Id,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Location.Y, // Latitude
        shelter.Location.X, // Longitude
        shelter.IsActive
    );
}
```

### 3. Dependency Flow

```
Domain ← Application ← Infrastructure ← Api
```

- **Domain**: No dependencies on other layers (pure business logic)
- **Application**: References Domain only (interfaces, DTOs, business rules)
- **Infrastructure**: References Domain + Application (implements interfaces)
- **Api**: References all layers (composition root)

**Dependency Injection Setup (in Api/Program.cs):**
```csharp
// Register services
builder.Services.AddScoped<IShelterService, ShelterService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
```

### 4. Entity Configuration

Keep entities in Domain layer clean - no data annotations for database mapping.

**Domain Entity (Clean):**
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

**Entity Configuration (Fluent API in Infrastructure):**
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

### 5. DTOs and Mapping

**DTO Naming Conventions:**

- **API Requests/Responses**: Use `-Request` and `-Response` suffixes
- **Internal DTOs**: Use `-Dto` suffix only for internal transfers (not going in/out of API)
- **Organize in folders**: Separate `Requests/` and `Responses/` folders

**Use record types for DTOs:**
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

// Shelter.Application/Requests/UpdateShelterRequest.cs
public record UpdateShelterRequest(
    string Name,
    string? Description,
    int Capacity,
    bool IsActive);

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

// Shelter.Application/DTOs/ShelterDto.cs (Internal use only - not API boundary)
namespace Shelter.Application.DTOs;

public record ShelterDto(
    Guid Id,
    string Name,
    int Capacity,
    Point Location);
```

**Folder Structure:**
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

**Mapping in Services (keep it simple):**
```csharp
// Map to Response (for API)
private static ShelterResponse MapToResponse(Domain.Shelters.Shelter shelter) => new(
    shelter.Id,
    shelter.Name,
    shelter.Description,
    shelter.Capacity,
    shelter.Location.Y, // Latitude
    shelter.Location.X, // Longitude
    shelter.IsActive
);

// Map to internal DTO (for inter-service communication)
private static ShelterDto MapToDto(Domain.Shelters.Shelter shelter) => new(
    shelter.Id,
    shelter.Name,
    shelter.Capacity,
    shelter.Location
);
```

### 6. Transaction Management

**Default Behavior - No Explicit Transactions Needed:**

EF Core automatically wraps `SaveChangesAsync()` in a transaction. For most operations, **explicit transactions are unnecessary**.

```csharp
// ✅ CORRECT - Single SaveChanges (implicit transaction)
public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, Guid bookerId)
{
    var booking = new Booking { /* ... */ };
    context.Bookings.Add(booking);
    
    // Add related entities - all saved atomically
    context.Notifications.Add(new Notification
    {
        Booking = booking,
        Message = "Booking confirmed"
    });
    
    await context.SaveChangesAsync(); // Everything commits or rolls back together
    
    return MapToResponse(booking);
}
```

**When to Use Explicit Transactions:**

Only use explicit transactions when you need **multiple SaveChangesAsync calls** or **specific isolation levels**:

```csharp
// ✅ Multiple SaveChanges operations that must be atomic
public async Task<TransferResult> TransferBookingAsync(Guid bookingId, Guid newShelterId)
{
    await using var transaction = await context.Database.BeginTransactionAsync();
    
    // First operation - update booking
    var booking = await context.Bookings.FindAsync(bookingId);
    booking.ShelterId = newShelterId;
    await context.SaveChangesAsync(); // First commit point
    
    // Second operation - create audit record
    context.AuditLogs.Add(new AuditLog { /* ... */ });
    await context.SaveChangesAsync(); // Second commit point
    
    await transaction.CommitAsync();
    return new TransferResult(Success: true);
}

// ✅ Specific isolation level required
public async Task ProcessHighPriorityBookingAsync(CreateBookingDto dto)
{
    await using var transaction = await context.Database.BeginTransactionAsync(
        IsolationLevel.Serializable);
    
    // Critical section requiring strict isolation
    await context.SaveChangesAsync();
    await transaction.CommitAsync();
}
```

**Cross-Service Transactions with IUnitOfWork:**

If you need transactions across multiple service calls (e.g., in a controller or orchestrator), use the IUnitOfWork pattern:

```csharp
// Shelter.Application/Abstractions/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> CommitChangesAsync(CancellationToken cancellationToken = default);
}

// Controller example
public async Task<IActionResult> CreateComplexBooking(ComplexBookingRequest request)
{
    await using var transaction = await context.Database.BeginTransactionAsync();
    
    var shelter = await shelterService.UpdateAvailabilityAsync(request.ShelterId);
    var booking = await bookingService.CreateAsync(request);
    await notificationService.SendConfirmationAsync(booking.Id);
    
    await unitOfWork.CommitChangesAsync();
    await transaction.CommitAsync();
    
    return Ok(booking);
}
```

### 7. Querying Patterns

**Use IQueryable for flexibility:**
```csharp
public async Task<List<ShelterResponse>> SearchAsync(ShelterSearchCriteria criteria)
{
    var query = context.Shelters
        .Where(s => s.IsActive)
        .AsQueryable();
    
    if (!string.IsNullOrEmpty(criteria.Name))
    {
        query = query.Where(s => s.Name.Contains(criteria.Name));
    }
    
    if (criteria.Location != null && criteria.RadiusKm > 0)
    {
        var radiusMeters = criteria.RadiusKm * 1000;
        query = query.Where(s => s.Location.Distance(criteria.Location) <= radiusMeters);
    }
    
    if (criteria.MinCapacity > 0)
    {
        query = query.Where(s => s.Capacity >= criteria.MinCapacity);
    }
    
    return await query
        .OrderBy(s => s.Name)
        .Select(s => new ShelterResponse(
            s.Id,
            s.Name,
            s.Description,
            s.Capacity,
            s.Location.Y,
            s.Location.X,
            s.IsActive))
        .ToListAsync();
}
```

### 8. Error Handling

**Let Exceptions Bubble - Don't Use Empty Try-Catch Blocks:**

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
        // Nothing useful happens here
        throw; // Just let it bubble naturally!
    }
}
```

✅ **CORRECT** - Services throw domain exceptions and let them bubble:
```csharp
// ✅ Let exceptions bubble naturally
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
    await context.SaveChangesAsync(); // EF handles transaction + rollback automatically
    
    return MapToResponse(booking);
}
```

**When Try-Catch IS Appropriate:**

Use try-catch when you're adding value:

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

// ✅ Handling specific exceptions differently
public async Task<BookingResponse> UpdateBookingAsync(Guid id, UpdateBookingRequest request)
{
    try
    {
        var booking = await context.Bookings.FindAsync(id);
        // ... update logic
        await context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        logger.LogWarning("Concurrency conflict updating booking {BookingId}", id);
        return await RetryWithLatestDataAsync(id, request);
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

**Global Exception Handling in Middleware:**

Handle all exceptions centrally with middleware or filters:

```csharp
// Shelter.Api/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Business rule violation");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "An error occurred" });
        }
    }
}
```

## Key Principles Summary

1. ✅ Services use DbContext directly (no repositories)
2. ✅ Interfaces in Application, implementations in Infrastructure
3. ✅ Primary constructors for dependency injection
4. ✅ Record types for DTOs
5. ✅ Fluent API for entity configuration
6. ✅ Keep Domain layer dependency-free
7. ✅ Use IQueryable for flexible queries
8. ✅ Business logic in services, not controllers
9. ✅ Let DbContext manage transactions (explicit only when needed)
10. ✅ Let exceptions bubble, handle globally in middleware
11. ✅ No try-catch-rethrow anti-patterns
12. ✅ Try-catch only when adding value (logging, transformation, specific handling)