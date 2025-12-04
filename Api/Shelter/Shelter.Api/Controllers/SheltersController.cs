using Microsoft.AspNetCore.Mvc;
using Shelter.Application.Interfaces;

namespace Shelter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SheltersController(IFileStorage fileStorage) : ControllerBase
{
    [HttpPost("test-upload")]
    public async Task<IActionResult> TestUpload()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("hello from shelter");
        using var ms = new MemoryStream(bytes);

        var url = await fileStorage.SaveShelterPictureAsync(Guid.NewGuid(), ms, "text/plain");
        return Ok(new { url });
    }
}