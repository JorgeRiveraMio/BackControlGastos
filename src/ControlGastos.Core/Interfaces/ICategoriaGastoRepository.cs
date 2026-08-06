using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface ICategoriaGastoRepository
{
    Task<IReadOnlyList<CategoriaGasto_Listar_DTO>> ObtenerActivasAsync(
        CancellationToken cancellationToken);
}
