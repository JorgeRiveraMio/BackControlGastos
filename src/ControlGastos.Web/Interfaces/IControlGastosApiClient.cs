using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Models.ViewModels;

namespace ControlGastos.Web.Interfaces;

public interface IControlGastosApiClient
{
    Task<ApiClientResult<IReadOnlyList<CategoriaViewModel>>> ObtenerCategoriasAsync(
        string accessToken,
        CancellationToken cancellationToken);
}
