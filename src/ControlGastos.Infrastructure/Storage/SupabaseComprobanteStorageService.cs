using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ControlGastos.Core.DTOs;
using ControlGastos.Core.Exceptions;
using ControlGastos.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControlGastos.Infrastructure.Storage;

public sealed class SupabaseComprobanteStorageService(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options,
    ILogger<SupabaseComprobanteStorageService> logger) : IComprobanteStorageService
{
    private const int DuracionUrlTemporalSegundos = 300;
    private readonly SupabaseOptions _options = options.Value;

    public async Task<ComprobanteSubido_DTO> SubirAsync(
        Guid idUsuario,
        long idGasto,
        Stream contenido,
        string nombreArchivo,
        string contentType,
        CancellationToken cancellationToken)
    {
        ValidarConfiguracion();

        var extension = ObtenerExtensionValida(nombreArchivo);
        var nombreLimpio = Path.GetFileName(nombreArchivo);
        var fechaActual = DateTimeOffset.UtcNow;
        var rutaArchivo = $"{idUsuario:D}/{fechaActual:yyyy}/{fechaActual:MM}/{idGasto}/{Guid.NewGuid():D}{extension}";
        using var solicitud = CrearSolicitud(HttpMethod.Post, $"storage/v1/object/{Escapar(_options.ComprobantesBucket)}/{EscaparRuta(rutaArchivo)}");
        using var cuerpo = new StreamContent(contenido);
        cuerpo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        solicitud.Content = cuerpo;
        solicitud.Headers.TryAddWithoutValidation("x-upsert", "false");

        try
        {
            using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
            if (!respuesta.IsSuccessStatusCode)
            {
                await LanzarErrorStorageAsync("subir", respuesta, cancellationToken);
            }
        }
        catch (HttpRequestException exception)
        {
            throw new StorageServiceException("No fue posible comunicarse con Supabase Storage.", exception);
        }

        return new ComprobanteSubido_DTO
        {
            RutaArchivo = rutaArchivo,
            NombreArchivo = nombreLimpio
        };
    }

    public async Task<string> ObtenerUrlTemporalAsync(string rutaArchivo, CancellationToken cancellationToken)
    {
        ValidarConfiguracion();

        using var solicitud = CrearSolicitud(
            HttpMethod.Post,
            $"storage/v1/object/sign/{Escapar(_options.ComprobantesBucket)}/{EscaparRuta(rutaArchivo)}");
        solicitud.Content = JsonContent.Create(new { expiresIn = DuracionUrlTemporalSegundos });

        try
        {
            using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
            if (!respuesta.IsSuccessStatusCode)
            {
                await LanzarErrorStorageAsync("generar la URL temporal", respuesta, cancellationToken);
            }

            var resultado = await respuesta.Content.ReadFromJsonAsync<FirmaUrlResponse>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(resultado?.SignedUrl))
            {
                throw new StorageServiceException("Supabase Storage no devolvió una URL temporal válida.");
            }

            return resultado.SignedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? resultado.SignedUrl
                : $"{_options.Url.TrimEnd('/')}/storage/v1{resultado.SignedUrl}";
        }
        catch (HttpRequestException exception)
        {
            throw new StorageServiceException("No fue posible comunicarse con Supabase Storage.", exception);
        }
    }

    public async Task EliminarAsync(string rutaArchivo, CancellationToken cancellationToken)
    {
        ValidarConfiguracion();

        using var solicitud = CrearSolicitud(HttpMethod.Delete, $"storage/v1/object/{Escapar(_options.ComprobantesBucket)}");
        solicitud.Content = JsonContent.Create(new { prefixes = new[] { rutaArchivo } });

        try
        {
            using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
            if (!respuesta.IsSuccessStatusCode)
            {
                await LanzarErrorStorageAsync("eliminar", respuesta, cancellationToken);
            }
        }
        catch (HttpRequestException exception)
        {
            throw new StorageServiceException("No fue posible comunicarse con Supabase Storage.", exception);
        }
    }

    private HttpRequestMessage CrearSolicitud(HttpMethod metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(metodo, $"{_options.Url.TrimEnd('/')}/{ruta}");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        solicitud.Headers.TryAddWithoutValidation("apikey", _options.SecretKey);
        return solicitud;
    }

    private async Task LanzarErrorStorageAsync(
        string operacion,
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        var detalle = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError(
            "Supabase Storage respondió {StatusCode} al intentar {Operacion}. Detalle: {Detalle}",
            (int)respuesta.StatusCode,
            operacion,
            detalle);
        throw new StorageServiceException($"No fue posible {operacion} el comprobante en Supabase Storage.");
    }

    private void ValidarConfiguracion()
    {
        if (string.IsNullOrWhiteSpace(_options.Url)
            || string.IsNullOrWhiteSpace(_options.SecretKey)
            || string.IsNullOrWhiteSpace(_options.ComprobantesBucket))
        {
            throw new StorageServiceException("La configuración de Supabase Storage está incompleta.");
        }
    }

    private static string ObtenerExtensionValida(string nombreArchivo)
    {
        var extension = Path.GetExtension(Path.GetFileName(nombreArchivo)).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".webp"
            ? extension
            : throw new StorageServiceException("El tipo de archivo no está permitido.");
    }

    private static string Escapar(string valor) => Uri.EscapeDataString(valor);

    private static string EscaparRuta(string ruta) => string.Join('/', ruta.Split('/').Select(Escapar));

    private sealed class FirmaUrlResponse
    {
        public string? SignedUrl { get; init; }
    }
}
