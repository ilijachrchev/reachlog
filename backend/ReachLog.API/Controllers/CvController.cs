using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReachLog.Application.DTOs.Cv;
using ReachLog.Application.Interfaces;
using System.Security.Claims;

namespace ReachLog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CvController : ControllerBase
{
    private readonly ICvService _cvService;

    public CvController(ICvService cvService)
    {
        _cvService = cvService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        var allowedTypes = new[]
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only PDF and Word documents are supported." });

        using var stream = file.OpenReadStream();
        var result = await _cvService.UploadAsync(stream, file.FileName, file.ContentType, GetUserId());
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _cvService.GetAsync(GetUserId());

        if (result == null)
            return NotFound(new { message = "No CV uploaded yet." });

        return Ok(result);
    }

    [HttpGet("file")]
    public async Task<IActionResult> GetFile()
    {
        var result = await _cvService.GetFileAsync(GetUserId());

        if (result == null)
            return NotFound(new { message = "No CV file stored." });

        return File(result.Value.bytes, result.Value.contentType, result.Value.fileName);
    }

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks()
    {
        var blocks = await _cvService.GetCvBlocksAsync(GetUserId());
        return Ok(blocks);
    }

    [HttpPost("suggest")]
    public async Task<IActionResult> GetSuggestions([FromBody] CvSuggestRequestDto request)
    {
        var suggestions = await _cvService.GetSuggestionsAsync(GetUserId(), request);
        return Ok(suggestions);
    }

    [HttpPost("export/docx")]
    public async Task<IActionResult> ExportDocx([FromBody] CvExportRequestDto request)
    {
        var bytes = await _cvService.ExportCvAsDocxAsync(request);
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "cv-edited.docx");
    }

    [HttpPost("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromBody] CvExportRequestDto request)
    {
        var bytes = await _cvService.ExportCvAsPdfAsync(request);
        return File(bytes, "application/pdf", "cv-edited.pdf");
    }
}
