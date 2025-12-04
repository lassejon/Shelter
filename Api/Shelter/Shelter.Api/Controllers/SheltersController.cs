using Microsoft.AspNetCore.Mvc;
using Shelter.Application.Interfaces;

namespace Shelter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SheltersController(IFileStorage fileStorage) : ControllerBase
{

}