using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface IGastoComprobanteService
{
    Task<ComprobanteSubido_DTO?> SubirAsync(
        Guid idUsuario,
        long idGasto,
        Stream contenido,
        string nombreArchivo,
        string contentType,
        CancellationToken cancellationToken);

    Task<ComprobanteUrl_DTO?> ObtenerUrlAsync(Guid idUsuario, long idGasto, CancellationToken cancellationToken);

    Task<bool> EliminarAsync(Guid idUsuario, long idGasto, CancellationToken cancellationToken);
}
