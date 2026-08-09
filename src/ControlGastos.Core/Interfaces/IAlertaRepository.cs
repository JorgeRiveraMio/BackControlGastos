using ControlGastos.Core.DTOs;
using ControlGastos.Core.Entities;

namespace ControlGastos.Core.Interfaces;

public interface IAlertaRepository
{
    Task<bool> ExisteAsync(Guid idUsuario, string codigoTipo, int anio, int mes, CancellationToken cancellationToken);
    Task<bool> CrearAsync(Alerta alerta, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alerta_Listar_DTO>> ListarAsync(Guid idUsuario, CancellationToken cancellationToken);
    Task MarcarLeidaAsync(Guid idUsuario, long idAlerta, CancellationToken cancellationToken);
    Task<int> ContarNoLeidasAsync(Guid idUsuario, CancellationToken cancellationToken);
}
