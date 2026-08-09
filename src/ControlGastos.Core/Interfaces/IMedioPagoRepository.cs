using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface IMedioPagoRepository
{
    Task<IReadOnlyList<MedioPago_Listar_DTO>> ObtenerActivosAsync(CancellationToken cancellationToken);
}
