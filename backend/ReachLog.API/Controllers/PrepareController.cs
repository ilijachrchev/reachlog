using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReachLog.Application.DTOs.Prepare;
using ReachLog.Application.Interfaces;
using System.Security.Claims;

namespace ReachLog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrepareController : ControllerBase
{
    private readonly IPrepareService _prepareService;

    public PrepareController(IPrepareService prepareService)
    {
        _prepareService = prepareService;
    }

    [HttpPost]
    public async Task<IActionResult> Prepare([FromBody] PrepareRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var result = await _prepareService.PrepareAsync(request.JobDescription, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
