using ControlGastos.Core.DTOs;
using ControlGastos.Core.Exceptions;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ocr")]
public sealed class OcrController(IReceiptOcrService receiptOcrService) : ControllerBase
{
    private const long MaximumFileSizeBytes = 5 * 1024 * 1024;

    [HttpPost("test")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumFileSizeBytes)]
    [ProducesResponseType(typeof(ReceiptOcrResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ReceiptOcrResult>> TestAsync(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var error = ValidateFile(file);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        try
        {
            await using var image = file!.OpenReadStream();
            var result = await receiptOcrService.AnalyzeAsync(
                image,
                file.FileName,
                file.ContentType,
                cancellationToken);
            return Ok(result);
        }
        catch (OcrServiceException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "No fue posible procesar el comprobante con el servicio OCR.");
        }
    }

    private static string? ValidateFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Debe adjuntar un archivo no vacío.";
        }

        if (file.Length > MaximumFileSizeBytes)
        {
            return "El archivo supera el tamaño máximo permitido de 5 MB.";
        }

        var validContentType = file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            || file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            || file.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase);
        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        var validExtension = extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);

        return validContentType && validExtension ? null : "El tipo de archivo no está permitido.";
    }
}
