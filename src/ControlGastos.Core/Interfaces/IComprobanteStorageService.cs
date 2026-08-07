using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface IComprobanteStorageService
{
    Task<ComprobanteSubido_DTO> SubirAsync(
        Guid idUsuario,
        long idGasto,
        Stream contenido,
        string nombreArchivo,
        string contentType,
        CancellationToken cancellationToken);

    Task<string> ObtenerUrlTemporalAsync(string rutaArchivo, CancellationToken cancellationToken);

    Task EliminarAsync(string rutaArchivo, CancellationToken cancellationToken);
}
