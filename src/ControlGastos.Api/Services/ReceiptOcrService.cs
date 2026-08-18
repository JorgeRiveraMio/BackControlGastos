using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ControlGastos.Core.DTOs;
using ControlGastos.Core.Exceptions;
using ControlGastos.Core.Interfaces;

namespace ControlGastos.Api.Services;

public sealed class ReceiptOcrService(
    HttpClient httpClient,
    ILogger<ReceiptOcrService> logger) : IReceiptOcrService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReceiptOcrResult> AnalyzeAsync(
        Stream image,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(image);

        if (MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            fileContent.Headers.ContentType = mediaType;
        }

        form.Add(fileContent, "file", Path.GetFileName(fileName));

        try
        {
            using var response = await httpClient.PostAsync("api/ocr/receipt", form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "El servicio OCR respondió {StatusCode}. Detalle: {Detail}",
                    (int)response.StatusCode,
                    detail);
                throw new OcrServiceException(
                    $"El servicio OCR respondió con el código HTTP {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<ReceiptOcrResult>(JsonOptions, cancellationToken);
            return result ?? throw new OcrServiceException("El servicio OCR devolvió una respuesta vacía.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException exception)
        {
            logger.LogWarning(exception, "La solicitud al servicio OCR excedió el tiempo de espera configurado.");
            throw new OcrServiceException("El servicio OCR excedió el tiempo de espera configurado.", exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "No fue posible comunicarse con el servicio OCR.");
            throw new OcrServiceException("No fue posible comunicarse con el servicio OCR.", exception);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "El servicio OCR devolvió JSON inválido.");
            throw new OcrServiceException("El servicio OCR devolvió una respuesta inválida.", exception);
        }
    }
}
