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
    public async Task<ApiClientResult<IReadOnlyList<CategoriaViewModel>>> ObtenerCategoriasAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "api/categorias");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new ApiClientResult<IReadOnlyList<CategoriaViewModel>> { IsUnauthorized = true };
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                logger.LogWarning("ControlGastos.Api respondió {StatusCode} al consultar categorías.", (int)respuesta.StatusCode);
                return new ApiClientResult<IReadOnlyList<CategoriaViewModel>>();
            }

            var contenido = await respuesta.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CategoriaViewModel>>>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancellationToken);

            return new ApiClientResult<IReadOnlyList<CategoriaViewModel>>
            {
                IsSuccess = contenido?.IsOk == true,
                Data = contenido?.Data
            };
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "No fue posible consultar ControlGastos.Api.");
            return new ApiClientResult<IReadOnlyList<CategoriaViewModel>>();
        }
    }
}
