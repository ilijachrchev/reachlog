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

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] ScrapeRequestDto request)
    {
        await _scraperService.RunAsync(request, GetUserId());
        var jobs = await _scraperService.GetJobsAsync(GetUserId(), null, null, null);
        return Ok(new { totalFound = jobs.Count });
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? jobType,
        [FromQuery] bool? remoteOnly,
        [FromQuery] int? minScore)
    {
        var result = await _scraperService.GetJobsAsync(GetUserId(), jobType, remoteOnly, minScore);
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

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _scraperService.GetStatusAsync(GetUserId());
        return Ok(result);
    }
}
