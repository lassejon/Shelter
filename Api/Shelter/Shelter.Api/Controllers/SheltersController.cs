using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shelter.Application.Requests;
using Shelter.Application.Responses;
using Shelter.Application.Services;
using System.Security.Claims;

namespace Shelter.Api.Controllers;

[ApiController]
[Route("api/v1/shelters")]
[Produces("application/json")]
public class SheltersController(
    IShelterService shelterService,
    ILogger<SheltersController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ShelterDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShelterDetailResponse>> Create([FromForm] CreateShelterRequest request)
    {
        var userId = GetUserId();

        // Convert IFormFile to FileUpload
        List<FileUpload>? fileUploads = null;
        if (request.Pictures != null && request.Pictures.Count > 0)
        {
            fileUploads = request.Pictures.Select(p => new FileUpload(
                p.OpenReadStream(),
                p.ContentType,
                p.FileName
            )).ToList();
        }

        var shelter = await shelterService.CreateAsync(request, userId, fileUploads);

        logger.LogInformation("User {UserId} created shelter {ShelterId}", userId, shelter.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { id = shelter.Id },
            shelter);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShelterDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShelterDetailResponse>> GetById(Guid id)
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
    [ProducesResponseType(typeof(List<ShelterSearchResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ShelterSearchResponse>>> Search([FromQuery] ShelterSearchCriteria criteria)
    {
        var shelters = await shelterService.SearchAsync(criteria);

        return Ok(shelters);
    }

    [HttpGet("{id}/bookings")]
    [ProducesResponseType(typeof(List<BookingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingResponse>>> GetBookings(
        Guid id,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to)
    {
        var bookings = await shelterService.GetBookingsAsync(id, from, to);
        return Ok(bookings);
    }

    [HttpGet("{id}/reviews")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetReviews(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var (reviews, totalCount) = await shelterService.GetReviewsAsync(id, page, pageSize);

        return Ok(new
        {
            reviews,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShelterRequest request)
    {
        var success = await shelterService.UpdateAsync(id, request);

        if (!success)
        {
            return NotFound();
        }

        logger.LogInformation("Updated shelter {ShelterId}", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await shelterService.DeleteAsync(id);

        if (!success)
        {
            return NotFound();
        }

        logger.LogInformation("Deleted shelter {ShelterId}", id);

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }
}