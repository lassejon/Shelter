# API Development Guidelines

## Primary Reference: Zalando REST API Guidelines

Follow the Zalando RESTful API Guidelines:
https://opensource.zalando.com/restful-api-guidelines/

Key sections to reference:
- General guidelines
- REST Basics - URLs
- REST Basics - JSON
- REST Basics - HTTP
- Hypermedia
- Data formats
- Common data types
- Common headers
- Proprietary headers
- API naming
- Resources
- HTTP requests
- HTTP status codes
- Performance
- Pagination
- Compatibility

## Controller-Based Architecture

**We use Controllers, NOT Minimal APIs.**

### Controller Structure

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
    
    [HttpGet]
    [ProducesResponseType(typeof(List<ShelterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ShelterDto>>> Search([FromQuery] ShelterSearchCriteria criteria)
    {
        var shelters = await shelterService.SearchAsync(criteria);
        return Ok(shelters);
    }
    
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ShelterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShelterDto>> Create([FromBody] CreateShelterDto dto)
    {
        var userId = User.GetUserId(); // Extension method
        var shelter = await shelterService.CreateAsync(dto, userId);
        
        return CreatedAtAction(
            nameof(GetById),
            new { id = shelter.Id },
            shelter);
    }
    
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShelterDto dto)
    {
        var userId = User.GetUserId();
        var result = await shelterService.UpdateAsync(id, dto, userId);
        
        return result ? NoContent() : NotFound();
    }
    
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var result = await shelterService.DeleteAsync(id, userId);
        
        return result ? NoContent() : NotFound();
    }
}
```

## API Design Rules

### 1. URL Design (Zalando)

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

### 2. HTTP Methods (Zalando)

- `GET` - Retrieve resource(s) (safe, idempotent, cacheable)
- `POST` - Create new resource (not idempotent)
- `PUT` - Replace entire resource (idempotent)
- `PATCH` - Partial update (use sparingly)
- `DELETE` - Remove resource (idempotent)

### 3. Response Status Codes (Zalando)

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
- `409 Conflict` - Request conflicts with current state (e.g., duplicate)
- `422 Unprocessable Entity` - Semantic errors in request

**Server Errors:**
- `500 Internal Server Error` - Unexpected server error
- `503 Service Unavailable` - Service temporarily unavailable

### 4. Response Structure

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

### 5. Pagination (Zalando)

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

**Controller example:**
```csharp
[HttpGet]
[ProducesResponseType(typeof(PagedResult<ShelterDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<PagedResult<ShelterDto>>> GetAll(
    [FromQuery] int limit = 20,
    [FromQuery] string? cursor = null)
{
    var result = await shelterService.GetPagedAsync(limit, cursor);
    return Ok(result);
}
```

### 6. Filtering and Sorting

**Filtering via query parameters:**
```
GET /api/v1/shelters?is_active=true&min_capacity=20
GET /api/v1/bookings?status=confirmed&start_date=2024-01-01
```

**Sorting:**
```
GET /api/v1/shelters?sort=name
GET /api/v1/shelters?sort=-created_at  (descending)
```

**Controller example:**
```csharp
[HttpGet]
public async Task<ActionResult<List<ShelterDto>>> Search(
    [FromQuery] bool? isActive = null,
    [FromQuery] int? minCapacity = null,
    [FromQuery] string? sort = null)
{
    var criteria = new ShelterSearchCriteria
    {
        IsActive = isActive,
        MinCapacity = minCapacity,
        SortBy = sort
    };
    
    var shelters = await shelterService.SearchAsync(criteria);
    return Ok(shelters);
}
```

### 7. Validation

Use Data Annotations or FluentValidation for request validation.

**Data Annotations:**
```csharp
public record CreateShelterDto(
    [Required, StringLength(200)] string Name,
    [StringLength(2000)] string? Description,
    [Range(1, 1000)] int Capacity,
    [Required, Range(-90, 90)] double Latitude,
    [Required, Range(-180, 180)] double Longitude,
    BookingPolicy BookingPolicy);
```

**FluentValidation (preferred):**
```csharp
public class CreateShelterDtoValidator : AbstractValidator<CreateShelterDto>
{
    public CreateShelterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Description)
            .MaximumLength(2000);
            
        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 1000);
            
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);
            
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);
    }
}
```

### 8. Authorization Patterns

**Use [Authorize] attribute:**
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

**Resource-based authorization:**
```csharp
[HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShelterDto dto)
{
    var userId = User.GetUserId();
    var shelter = await shelterService.GetByIdAsync(id);
    
    if (shelter == null)
        return NotFound();
    
    // Check if user owns the shelter
    if (shelter.OwnerId != userId)
        return Forbid();
    
    await shelterService.UpdateAsync(id, dto);
    return NoContent();
}
```

### 9. Async/Await

**ALL database operations must be async:**
```csharp
// ✅ CORRECT
[HttpGet("{id}")]
public async Task<ActionResult<ShelterDto>> GetById(Guid id)
{
    var shelter = await shelterService.GetByIdAsync(id);
    return shelter == null ? NotFound() : Ok(shelter);
}

// ❌ WRONG - Don't block async operations
[HttpGet("{id}")]
public ActionResult<ShelterDto> GetById(Guid id)
{
    var shelter = shelterService.GetByIdAsync(id).Result; // BAD!
    return shelter == null ? NotFound() : Ok(shelter);
}
```

### 10. Logging

**Inject ILogger and log important operations:**
```csharp
public class SheltersController(
    IShelterService shelterService,
    ILogger<SheltersController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ShelterDto>> Create([FromBody] CreateShelterDto dto)
    {
        var userId = User.GetUserId();
        
        logger.LogInformation(
            "User {UserId} is creating a shelter with name {ShelterName}",
            userId,
            dto.Name);
        
        try
        {
            var shelter = await shelterService.CreateAsync(dto, userId);
            
            logger.LogInformation(
                "Successfully created shelter {ShelterId} for user {UserId}",
                shelter.Id,
                userId);
            
            return CreatedAtAction(nameof(GetById), new { id = shelter.Id }, shelter);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error creating shelter for user {UserId}",
                userId);
            throw;
        }
    }
}
```

### 11. API Versioning

Use URL versioning:
```
/api/v1/shelters
/api/v2/shelters
```

**Setup in Program.cs:**
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
```

**Controller versioning:**
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SheltersController : ControllerBase
{
    // v1 implementation
}

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
public class SheltersV2Controller : ControllerBase
{
    // v2 implementation with breaking changes
}
```

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
10. ✅ Input validation (Data Annotations or FluentValidation)

## Common Patterns

### Search/Filter Pattern
```csharp
[HttpGet]
public async Task<ActionResult<List<ShelterDto>>> Search(
    [FromQuery] ShelterSearchCriteria criteria)
{
    var results = await shelterService.SearchAsync(criteria);
    return Ok(results);
}
```

### Nested Resources Pattern
```csharp
// Get bookings for a specific shelter
[HttpGet("/api/v1/shelters/{shelterId}/bookings")]
public async Task<ActionResult<List<BookingDto>>> GetShelterBookings(Guid shelterId)
{
    var bookings = await bookingService.GetByShelterIdAsync(shelterId);
    return Ok(bookings);
}
```

### Bulk Operations Pattern
```csharp
[HttpPost("bulk")]
public async Task<ActionResult<BulkOperationResult>> CreateBulk(
    [FromBody] List<CreateShelterDto> dtos)
{
    var result = await shelterService.CreateBulkAsync(dtos);
    return Ok(result);
}
```

## Anti-Patterns to Avoid

❌ Don't expose entities directly - always use DTOs
❌ Don't put business logic in controllers - use services
❌ Don't use `Result` or `.Wait()` on async operations
❌ Don't return 200 for errors - use appropriate status codes
❌ Don't forget to validate input
❌ Don't create God controllers - keep them focused
❌ Don't use Minimal APIs - use Controllers for consistency