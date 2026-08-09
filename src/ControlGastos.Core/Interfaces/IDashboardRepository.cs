using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardResumenMensual_DTO> ObtenerResumenMensualAsync(
        Guid idUsuario, int anio, int mes, CancellationToken cancellationToken);
}
