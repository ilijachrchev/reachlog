using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReachLog.Application.DTOs.Scraper;
using ReachLog.Application.Interfaces;
using System.Security.Claims;

namespace ReachLog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScraperController : ControllerBase
{
    private readonly IScraperService _scraperService;

    public ScraperController(IScraperService scraperService)
    {
        _scraperService = scraperService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string? GetUserRole() =>
        User.FindFirstValue("role");

    [HttpPost("run")]
    public async Task<IActionResult> Run()
    {
        if (GetUserRole() != "Admin")
            return Forbid();
        await _scraperService.RunAsync(GetUserId());
        var info = await _scraperService.GetInfoAsync();
        return Ok(new { totalFound = info.TotalJobsInFeed });
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? jobType,
        [FromQuery] bool? remoteOnly)
    {
        var result = await _scraperService.GetJobsAsync(GetUserId(), jobType, remoteOnly);
        return Ok(result);
    }

    [HttpPost("jobs/{id}/import")]
    public async Task<IActionResult> ImportJob(Guid id)
    {
        try
        {
            var result = await _scraperService.ImportJobAsync(id, GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("info")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInfo()
    {
        var result = await _scraperService.GetInfoAsync();
        return Ok(result);
    }

    [HttpGet("preference")]
    public async Task<IActionResult> GetPreference()
    {
        var result = await _scraperService.GetPreferenceAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("preference")]
    public async Task<IActionResult> SavePreference([FromBody] UserJobPreferenceDto dto)
    {
        var result = await _scraperService.SavePreferenceAsync(dto, GetUserId());
        return Ok(result);
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestScrape()
    {
        await _scraperService.RequestScrapeAsync(GetUserId());
        return Ok();
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        if (GetUserRole() != "Admin")
            return Forbid();
        var result = await _scraperService.GetPendingRequestsAsync();
        return Ok(result);
    }
}
