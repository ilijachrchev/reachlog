using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReachLog.Application.Interfaces;

namespace ReachLog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParseController : ControllerBase
{
    private readonly IParseService _parseService;

    public ParseController(IParseService parseService)
    {
        _parseService = parseService;
    }

    [HttpPost]
    public async Task<IActionResult> Parse([FromBody] ParseRequestDto request)
    {
        try
        {
            var result = await _parseService.ParseMessageAsync(request.RawMessage);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class ParseRequestDto
{
    public string RawMessage { get; set; } = string.Empty;
}