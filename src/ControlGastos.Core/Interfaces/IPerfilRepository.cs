using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface IPerfilRepository
{
    Task<PerfilFinanciero_DTO?> ObtenerAsync(Guid idUsuario, CancellationToken cancellationToken);
    Task GuardarAsync(Guid idUsuario, PerfilFinanciero_Actualizar_DTO dto, CancellationToken cancellationToken);
}
