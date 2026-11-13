using System.Net;
using DocumentManagement.API.Models;
using DocumentManagement.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes + 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentUploadResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<DocumentUploadResultDto>> UploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(CreateProblemDetails(StatusCodes.Status400BadRequest, "Missing file", "A file upload is required."));
        }

        if (file.Length == 0)
        {
            return BadRequest(CreateProblemDetails(StatusCodes.Status400BadRequest, "Empty file", "The uploaded file is empty."));
        }

        if (file.Length > MaxUploadSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, CreateProblemDetails(StatusCodes.Status413PayloadTooLarge, "File too large", "The uploaded file exceeds the 10 MB limit."));
        }

        try
        {
            var result = await _documentService.UploadAsync(file, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (DocumentValidationException ex)
        {
            return BadRequest(CreateProblemDetails(StatusCodes.Status400BadRequest, "Invalid file", ex.Message));
        }
        catch (DocumentStorageException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(StatusCodes.Status500InternalServerError, "Storage error", ex.Message));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetAllAsync(cancellationToken);
        return Ok(documents);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound(CreateProblemDetails(StatusCodes.Status404NotFound, "Document not found", $"Document '{id}' was not found."));
        }

        return Ok(document);
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _documentService.DownloadAsync(id, cancellationToken);
            return File(download.FileStream, download.ContentType, download.FileName);
        }
        catch (DocumentNotFoundException)
        {
            return NotFound(CreateProblemDetails(StatusCodes.Status404NotFound, "Document not found", $"Document '{id}' was not found."));
        }
        catch (DocumentFileMissingException)
        {
            return NotFound(CreateProblemDetails(StatusCodes.Status404NotFound, "Document file missing", $"The file for document '{id}' is missing."));
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (DocumentNotFoundException)
        {
            return NotFound(CreateProblemDetails(StatusCodes.Status404NotFound, "Document not found", $"Document '{id}' was not found."));
        }
        catch (DocumentStorageException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(StatusCodes.Status500InternalServerError, "Delete failed", ex.Message));
        }
    }

    private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
    }
}
