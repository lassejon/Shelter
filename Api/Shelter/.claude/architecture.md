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
    Task<ShelterDto> CreateAsync(CreateShelterDto dto, Guid ownerId);
    Task<ShelterDto?> GetByIdAsync(Guid id);
    Task<List<ShelterDto>> SearchAsync(ShelterSearchCriteria criteria);
    Task<bool> UpdateAsync(Guid id, UpdateShelterDto dto);
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
    public async Task<ShelterDto> CreateAsync(CreateShelterDto dto, Guid ownerId)
    {
        var shelter = new Domain.Shelters.Shelter
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = dto.Name,
            Description = dto.Description,
            Capacity = dto.Capacity,
            Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 },
            BookingPolicy = dto.BookingPolicy,
            IsActive = true
        };
        
        context.Shelters.Add(shelter);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Created shelter {ShelterId} for owner {OwnerId}", shelter.Id, ownerId);
        
        return MapToDto(shelter);
    }
    
    public async Task<ShelterDto?> GetByIdAsync(Guid id)
    {
        var shelter = await context.Shelters
            .Include(s => s.Pictures)
            .Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        return shelter == null ? null : MapToDto(shelter);
    }
    
    private static ShelterDto MapToDto(Domain.Shelters.Shelter shelter) => new(
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

**Use record types for DTOs:**
```csharp
// Shelter.Application/DTOs/ShelterDto.cs
namespace Shelter.Application.DTOs;

public record ShelterDto(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    bool IsActive);

public record CreateShelterDto(
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    BookingPolicy BookingPolicy);

public record UpdateShelterDto(
    string Name,
    string? Description,
    int Capacity,
    bool IsActive);
```

**Mapping in Services (keep it simple):**
```csharp
// Simple mapping methods in service
private static ShelterDto MapToDto(Domain.Shelters.Shelter shelter) => new(
    shelter.Id,
    shelter.Name,
    shelter.Description,
    shelter.Capacity,
    shelter.Location.Y,
    shelter.Location.X,
    shelter.IsActive
);
```

### 6. Transaction Management

DbContext handles transactions automatically on `SaveChangesAsync()`.

**For complex operations:**
```csharp
public async Task<BookingDto> CreateBookingWithNotificationAsync(CreateBookingDto dto)
{
    await using var transaction = await context.Database.BeginTransactionAsync();
    
    try
    {
        var booking = new Booking { /* ... */ };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        
        // Additional operations...
        
        await transaction.CommitAsync();
        return MapToDto(booking);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 7. Querying Patterns

**Use IQueryable for flexibility:**
```csharp
public async Task<List<ShelterDto>> SearchAsync(ShelterSearchCriteria criteria)
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
        .Select(s => new ShelterDto(
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

**Service layer returns results or throws domain exceptions:**
```csharp
public async Task<BookingDto> CreateBookingAsync(CreateBookingDto dto, Guid bookerId)
{
    var shelter = await context.Shelters.FindAsync(dto.ShelterId)
        ?? throw new NotFoundException($"Shelter {dto.ShelterId} not found");
    
    if (!shelter.IsActive)
    {
        throw new BusinessRuleException("Cannot book inactive shelter");
    }
    
    var hasConflict = await context.Bookings
        .AnyAsync(b => b.ShelterId == dto.ShelterId
            && b.Status != BookingStatus.Cancelled
            && b.StartUtc < dto.EndUtc
            && b.EndUtc > dto.StartUtc);
    
    if (hasConflict)
    {
        throw new BookingConflictException("Shelter is already booked for this period");
    }
    
    // Create booking...
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
9. ✅ Let DbContext manage transactions
10. ✅ Throw domain exceptions, handle in middleware