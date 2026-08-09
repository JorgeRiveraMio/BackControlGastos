using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Models.ViewModels;

namespace ControlGastos.Web.Services;

public sealed class ControlGastosApiClient(
    HttpClient httpClient,
    ILogger<ControlGastosApiClient> logger) : IControlGastosApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiClientResult<IReadOnlyList<GastoListItemViewModel>>> ObtenerGastosAsync(
        string accessToken, int anio, int mes, CancellationToken cancellationToken) =>
        EnviarAsync<IReadOnlyList<GastoListItemViewModel>>(
            CrearSolicitud(HttpMethod.Get, $"api/gastos?anio={anio}&mes={mes}", accessToken), cancellationToken);

    public Task<ApiClientResult<GastoListItemViewModel>> ObtenerGastoAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken) =>
        EnviarAsync<GastoListItemViewModel>(
            CrearSolicitud(HttpMethod.Get, $"api/gastos/{idGasto}", accessToken), cancellationToken);

    public Task<ApiClientResult<GastoRegistradoViewModel>> RegistrarGastoAsync(
        string accessToken, GastoApiRequest gasto, CancellationToken cancellationToken) =>
        EnviarAsync<GastoRegistradoViewModel>(CrearSolicitudJson(HttpMethod.Post, "api/gastos", accessToken, gasto), cancellationToken);

    public Task<ApiClientResult<object>> ActualizarGastoAsync(
        string accessToken, long idGasto, GastoApiRequest gasto, CancellationToken cancellationToken) =>
        EnviarAsync<object>(CrearSolicitudJson(HttpMethod.Put, $"api/gastos/{idGasto}", accessToken, gasto), cancellationToken);

    public Task<ApiClientResult<object>> AnularGastoAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken) =>
        EnviarAsync<object>(CrearSolicitud(HttpMethod.Delete, $"api/gastos/{idGasto}", accessToken), cancellationToken);

    public Task<ApiClientResult<IReadOnlyList<CategoriaViewModel>>> ObtenerCategoriasAsync(
        string accessToken, CancellationToken cancellationToken) =>
        EnviarAsync<IReadOnlyList<CategoriaViewModel>>(
            CrearSolicitud(HttpMethod.Get, "api/categorias", accessToken), cancellationToken);

    public Task<ApiClientResult<IReadOnlyList<MedioPagoViewModel>>> ObtenerMediosPagoAsync(
        string accessToken, CancellationToken cancellationToken) =>
        EnviarAsync<IReadOnlyList<MedioPagoViewModel>>(
            CrearSolicitud(HttpMethod.Get, "api/medios-pago", accessToken), cancellationToken);

    public async Task<ApiClientResult<object>> SubirComprobanteAsync(
        string accessToken, long idGasto, Stream contenidoArchivo, string nombreArchivo, string contentType,
        CancellationToken cancellationToken)
    {
        using var solicitud = CrearSolicitud(HttpMethod.Post, $"api/gastos/{idGasto}/comprobante", accessToken);
        using var formulario = new MultipartFormDataContent();
        using var contenido = new StreamContent(contenidoArchivo);
        contenido.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        formulario.Add(contenido, "archivo", nombreArchivo);
        solicitud.Content = formulario;
        return await EnviarAsync<object>(solicitud, cancellationToken);
    }

    public Task<ApiClientResult<ComprobanteUrlViewModel>> ObtenerComprobanteAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken) =>
        EnviarAsync<ComprobanteUrlViewModel>(
            CrearSolicitud(HttpMethod.Get, $"api/gastos/{idGasto}/comprobante", accessToken), cancellationToken);

    public Task<ApiClientResult<object>> EliminarComprobanteAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken) =>
        EnviarAsync<object>(
            CrearSolicitud(HttpMethod.Delete, $"api/gastos/{idGasto}/comprobante", accessToken), cancellationToken);

    public Task<ApiClientResult<PerfilFinancieroApiModel>> ObtenerPerfilAsync(string accessToken, CancellationToken cancellationToken) =>
        EnviarAsync<PerfilFinancieroApiModel>(CrearSolicitud(HttpMethod.Get, "api/perfil", accessToken), cancellationToken);

    public Task<ApiClientResult<object>> GuardarPerfilAsync(string accessToken, PerfilFinancieroActualizarApiModel perfil, CancellationToken cancellationToken) =>
        EnviarAsync<object>(CrearSolicitudJson(HttpMethod.Put, "api/perfil", accessToken, perfil), cancellationToken);

    public Task<ApiClientResult<DashboardResumenApiModel>> ObtenerDashboardAsync(string accessToken, int anio, int mes, CancellationToken cancellationToken) =>
        EnviarAsync<DashboardResumenApiModel>(CrearSolicitud(HttpMethod.Get, $"api/dashboard/resumen?anio={anio}&mes={mes}", accessToken), cancellationToken);

    public Task<ApiClientResult<IReadOnlyList<AlertaViewModel>>> ObtenerAlertasAsync(string accessToken, CancellationToken cancellationToken) =>
        EnviarAsync<IReadOnlyList<AlertaViewModel>>(CrearSolicitud(HttpMethod.Get, "api/alertas", accessToken), cancellationToken);

    public Task<ApiClientResult<int>> ObtenerCantidadAlertasNoLeidasAsync(string accessToken, CancellationToken cancellationToken) =>
        EnviarAsync<int>(CrearSolicitud(HttpMethod.Get, "api/alertas/no-leidas/count", accessToken), cancellationToken);

    public Task<ApiClientResult<object>> MarcarAlertaLeidaAsync(string accessToken, long idAlerta, CancellationToken cancellationToken) =>
        EnviarAsync<object>(CrearSolicitud(HttpMethod.Put, $"api/alertas/{idAlerta}/leida", accessToken), cancellationToken);

    private static HttpRequestMessage CrearSolicitud(HttpMethod metodo, string uri, string accessToken)
    {
        var solicitud = new HttpRequestMessage(metodo, uri);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return solicitud;
    }

    private static HttpRequestMessage CrearSolicitudJson<T>(HttpMethod metodo, string uri, string accessToken, T datos)
    {
        var solicitud = CrearSolicitud(metodo, uri, accessToken);
        solicitud.Content = JsonContent.Create(datos, options: JsonOptions);
        return solicitud;
    }

    private async Task<ApiClientResult<T>> EnviarAsync<T>(HttpRequestMessage solicitud, CancellationToken cancellationToken)
    {
        using (solicitud)
        {
            try
            {
                using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
                if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new ApiClientResult<T> { IsUnauthorized = true };
                }

                var contenido = await respuesta.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, cancellationToken);
                if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ApiClientResult<T> { IsNotFound = true, ErrorMessage = contenido?.Message };
                }

                if (!respuesta.IsSuccessStatusCode || contenido?.IsOk != true)
                {
                    logger.LogWarning("ControlGastos.Api respondiÃ³ {StatusCode} a {Method} {Uri}.",
                        (int)respuesta.StatusCode, solicitud.Method, solicitud.RequestUri);
                    return new ApiClientResult<T> { ErrorMessage = contenido?.Message };
                }

                return new ApiClientResult<T> { IsSuccess = true, Data = contenido.Data, ErrorMessage = contenido.Message };
            }
            catch (HttpRequestException exception)
            {
                logger.LogError(exception, "No fue posible comunicar con ControlGastos.Api.");
                return new ApiClientResult<T> { ErrorMessage = "No fue posible comunicarse con la API." };
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "ControlGastos.Api devolviÃ³ una respuesta no vÃ¡lida.");
                return new ApiClientResult<T> { ErrorMessage = "La API devolviÃ³ una respuesta no vÃ¡lida." };
            }
        }
    }
}
