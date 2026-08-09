using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Models.ViewModels;

namespace ControlGastos.Web.Interfaces;

public interface IControlGastosApiClient
{
    Task<ApiClientResult<IReadOnlyList<GastoListItemViewModel>>> ObtenerGastosAsync(
        string accessToken, int anio, int mes, CancellationToken cancellationToken);

    Task<ApiClientResult<GastoListItemViewModel>> ObtenerGastoAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken);

    Task<ApiClientResult<GastoRegistradoViewModel>> RegistrarGastoAsync(
        string accessToken, GastoApiRequest gasto, CancellationToken cancellationToken);

    Task<ApiClientResult<object>> ActualizarGastoAsync(
        string accessToken, long idGasto, GastoApiRequest gasto, CancellationToken cancellationToken);

    Task<ApiClientResult<object>> AnularGastoAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken);

    Task<ApiClientResult<IReadOnlyList<CategoriaViewModel>>> ObtenerCategoriasAsync(
        string accessToken,
        CancellationToken cancellationToken);

    Task<ApiClientResult<IReadOnlyList<MedioPagoViewModel>>> ObtenerMediosPagoAsync(
        string accessToken, CancellationToken cancellationToken);

    Task<ApiClientResult<object>> SubirComprobanteAsync(
        string accessToken, long idGasto, Stream contenido, string nombreArchivo, string contentType,
        CancellationToken cancellationToken);

    Task<ApiClientResult<ComprobanteUrlViewModel>> ObtenerComprobanteAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken);

    Task<ApiClientResult<object>> EliminarComprobanteAsync(
        string accessToken, long idGasto, CancellationToken cancellationToken);

    Task<ApiClientResult<PerfilFinancieroApiModel>> ObtenerPerfilAsync(string accessToken, CancellationToken cancellationToken);
    Task<ApiClientResult<object>> GuardarPerfilAsync(string accessToken, PerfilFinancieroActualizarApiModel perfil, CancellationToken cancellationToken);
    Task<ApiClientResult<DashboardResumenApiModel>> ObtenerDashboardAsync(string accessToken, int anio, int mes, CancellationToken cancellationToken);
}
