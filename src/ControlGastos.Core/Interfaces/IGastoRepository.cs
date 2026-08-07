using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface IGastoRepository
{
    Task<long> RegistrarAsync(Gasto_Registrar_DTO dto, CancellationToken cancellationToken);

    Task<IReadOnlyList<Gasto_Listar_DTO>> ObtenerPorMesAsync(
        Guid idUsuario,
        int anio,
        int mes,
        CancellationToken cancellationToken);

    Task<Gasto_Listar_DTO?> ObtenerPorIdAsync(
        long idGasto,
        Guid idUsuario,
        CancellationToken cancellationToken);

    Task<bool> ActualizarAsync(
        long idGasto,
        Guid idUsuario,
        Gasto_Actualizar_DTO dto,
        CancellationToken cancellationToken);

    Task<bool> AnularAsync(long idGasto, Guid idUsuario, CancellationToken cancellationToken);
}
