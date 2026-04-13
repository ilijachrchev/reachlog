using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReachLog.Application.Interfaces;
using System.Security.Claims;

namespace ReachLog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NudgesController : ControllerBase
{
    private readonly INudgeService _nudgeService;

    public NudgesController(INudgeService nudgeService)
    {
        _nudgeService = nudgeService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _nudgeService.GetNudgesAsync(userId);
        return Ok(result);
    }
}