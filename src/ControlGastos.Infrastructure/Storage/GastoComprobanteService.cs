using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ControlGastos.Infrastructure.Storage;

public sealed class GastoComprobanteService(
    IGastoRepository gastoRepository,
    IComprobanteStorageService storageService,
    ILogger<GastoComprobanteService> logger) : IGastoComprobanteService
{
    private const int DuracionUrlTemporalSegundos = 300;

    public async Task<ComprobanteSubido_DTO?> SubirAsync(
        Guid idUsuario,
        long idGasto,
        Stream contenido,
        string nombreArchivo,
        string contentType,
        CancellationToken cancellationToken)
    {
        var comprobanteAnterior = await gastoRepository.ObtenerComprobanteAsync(idGasto, idUsuario, cancellationToken);
        if (comprobanteAnterior is null)
        {
            return null;
        }

        var comprobanteNuevo = await storageService.SubirAsync(
            idUsuario,
            idGasto,
            contenido,
            nombreArchivo,
            contentType,
            cancellationToken);

        try
        {
            var actualizado = await gastoRepository.ActualizarComprobanteAsync(
                idGasto,
                idUsuario,
                comprobanteNuevo,
                cancellationToken);
            if (!actualizado)
            {
                await EliminarNuevoTrasFalloAsync(comprobanteNuevo.RutaArchivo, cancellationToken);
                return null;
            }
        }
        catch
        {
            await EliminarNuevoTrasFalloAsync(comprobanteNuevo.RutaArchivo, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(comprobanteAnterior.RutaArchivo))
        {
            try
            {
                await storageService.EliminarAsync(comprobanteAnterior.RutaArchivo, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No se pudo eliminar el comprobante anterior del gasto {IdGasto}.", idGasto);
            }
        }

        return comprobanteNuevo;
    }

    public async Task<ComprobanteUrl_DTO?> ObtenerUrlAsync(Guid idUsuario, long idGasto, CancellationToken cancellationToken)
    {
        var comprobante = await gastoRepository.ObtenerComprobanteAsync(idGasto, idUsuario, cancellationToken);
        if (string.IsNullOrWhiteSpace(comprobante?.RutaArchivo))
        {
            return null;
        }

        var url = await storageService.ObtenerUrlTemporalAsync(comprobante.RutaArchivo, cancellationToken);
        return new ComprobanteUrl_DTO
        {
            Url = url,
            ExpiraEnSegundos = DuracionUrlTemporalSegundos
        };
    }

    public async Task<bool> EliminarAsync(Guid idUsuario, long idGasto, CancellationToken cancellationToken)
    {
        var comprobante = await gastoRepository.ObtenerComprobanteAsync(idGasto, idUsuario, cancellationToken);
        if (string.IsNullOrWhiteSpace(comprobante?.RutaArchivo))
        {
            return false;
        }

        await storageService.EliminarAsync(comprobante.RutaArchivo, cancellationToken);
        return await gastoRepository.EliminarComprobanteAsync(idGasto, idUsuario, cancellationToken);
    }

    private async Task EliminarNuevoTrasFalloAsync(string rutaArchivo, CancellationToken cancellationToken)
    {
        try
        {
            await storageService.EliminarAsync(rutaArchivo, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo limpiar el nuevo comprobante tras un fallo de base de datos.");
        }
    }
}
