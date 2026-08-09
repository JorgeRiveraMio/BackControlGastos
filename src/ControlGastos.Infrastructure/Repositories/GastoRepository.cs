using ControlGastos.Core.DTOs;
using ControlGastos.Core.Entities;
using ControlGastos.Core.Exceptions;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Repositories;

public sealed class GastoRepository(ControlGastosDbContext dbContext) : IGastoRepository
{
    public async Task<long> RegistrarAsync(
        Guid idUsuario,
        Gasto_Registrar_DTO dto,
        CancellationToken cancellationToken)
    {
        await ValidarCategoriaYMedioPagoAsync(dto.IdCategoriaGasto, dto.IdMedioPago, cancellationToken);

        var gasto = new Gasto
        {
            IdUsuario = idUsuario,
            IdCategoriaGasto = dto.IdCategoriaGasto,
            IdMedioPago = dto.IdMedioPago,
            CodEstado = "CONFIRMADO",
            Monto = dto.Monto,
            FechaGasto = dto.FechaGasto.ToUniversalTime(),
            NombreComercio = dto.NombreComercio,
            Descripcion = dto.Descripcion,
            CodOrigen = "WEB",
            FechaRegistro = DateTimeOffset.UtcNow
        };

        dbContext.Gastos.Add(gasto);
        await dbContext.SaveChangesAsync(cancellationToken);

        return gasto.IdGasto;
    }

    public async Task<IReadOnlyList<Gasto_Listar_DTO>> ObtenerPorMesAsync(
        Guid idUsuario,
        int anio,
        int mes,
        CancellationToken cancellationToken)
    {
        var (fechaInicioUtc, fechaFinUtc) = FechaLima.ObtenerLimitesMensualesUtc(anio, mes);

        return await CrearConsultaListado()
            .Where(gasto => gasto.IdUsuario == idUsuario
                && gasto.FechaGasto >= fechaInicioUtc
                && gasto.FechaGasto < fechaFinUtc
                && gasto.CodEstado != "ANULADO")
            .OrderByDescending(gasto => gasto.FechaGasto)
            .ToListAsync(cancellationToken);
    }

    public Task<Gasto_Listar_DTO?> ObtenerPorIdAsync(
        long idGasto,
        Guid idUsuario,
        CancellationToken cancellationToken)
    {
        return CrearConsultaListado()
            .SingleOrDefaultAsync(gasto => gasto.IdGasto == idGasto && gasto.IdUsuario == idUsuario, cancellationToken);
    }

    public async Task<bool> ActualizarAsync(
        long idGasto,
        Guid idUsuario,
        Gasto_Actualizar_DTO dto,
        CancellationToken cancellationToken)
    {
        await ValidarCategoriaYMedioPagoAsync(dto.IdCategoriaGasto, dto.IdMedioPago, cancellationToken);

        var gasto = await dbContext.Gastos.SingleOrDefaultAsync(
            gasto => gasto.IdGasto == idGasto && gasto.IdUsuario == idUsuario,
            cancellationToken);

        if (gasto is null)
        {
            return false;
        }

        gasto.IdCategoriaGasto = dto.IdCategoriaGasto;
        gasto.IdMedioPago = dto.IdMedioPago;
        gasto.Monto = dto.Monto;
        gasto.FechaGasto = dto.FechaGasto.ToUniversalTime();
        gasto.NombreComercio = dto.NombreComercio;
        gasto.Descripcion = dto.Descripcion;
        gasto.FechaActualizacion = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AnularAsync(long idGasto, Guid idUsuario, CancellationToken cancellationToken)
    {
        var gasto = await dbContext.Gastos.SingleOrDefaultAsync(
            gasto => gasto.IdGasto == idGasto && gasto.IdUsuario == idUsuario,
            cancellationToken);

        if (gasto is null)
        {
            return false;
        }

        gasto.CodEstado = "ANULADO";
        gasto.FechaActualizacion = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<GastoComprobante_DTO?> ObtenerComprobanteAsync(
        long idGasto,
        Guid idUsuario,
        CancellationToken cancellationToken)
    {
        return dbContext.Gastos
            .AsNoTracking()
            .Where(gasto => gasto.IdGasto == idGasto && gasto.IdUsuario == idUsuario)
            .Select(gasto => new GastoComprobante_DTO
            {
                RutaArchivo = gasto.RutaComprobante,
                NombreArchivo = gasto.NombreComprobante
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ActualizarComprobanteAsync(
        long idGasto,
        Guid idUsuario,
        ComprobanteSubido_DTO comprobante,
        CancellationToken cancellationToken)
    {
        var gasto = await ObtenerGastoParaComprobanteAsync(idGasto, idUsuario, cancellationToken);
        if (gasto is null)
        {
            return false;
        }

        gasto.RutaComprobante = comprobante.RutaArchivo;
        gasto.NombreComprobante = comprobante.NombreArchivo;
        gasto.FechaActualizacion = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EliminarComprobanteAsync(long idGasto, Guid idUsuario, CancellationToken cancellationToken)
    {
        var gasto = await ObtenerGastoParaComprobanteAsync(idGasto, idUsuario, cancellationToken);
        if (gasto is null)
        {
            return false;
        }

        gasto.RutaComprobante = null;
        gasto.NombreComprobante = null;
        gasto.FechaActualizacion = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Gasto_Listar_DTO> CrearConsultaListado()
    {
        return from gasto in dbContext.Gastos.AsNoTracking()
               join categoria in dbContext.CategoriasGasto.AsNoTracking()
                   on gasto.IdCategoriaGasto equals categoria.IdCategoriaGasto
               join medioPago in dbContext.MediosPago.AsNoTracking()
                   on gasto.IdMedioPago equals (int?)medioPago.IdMedioPago into mediosPago
               from medioPago in mediosPago.DefaultIfEmpty()
               select new Gasto_Listar_DTO
               {
                   IdGasto = gasto.IdGasto,
                   IdUsuario = gasto.IdUsuario,
                   IdCategoriaGasto = gasto.IdCategoriaGasto,
                   NombreCategoria = categoria.Nombre,
                   IdMedioPago = gasto.IdMedioPago,
                   NombreMedioPago = medioPago == null ? null : medioPago.Nombre,
                   Monto = gasto.Monto,
                   FechaGasto = gasto.FechaGasto,
                   NombreComercio = gasto.NombreComercio,
                   Descripcion = gasto.Descripcion,
                   CodEstado = gasto.CodEstado,
                   CodOrigen = gasto.CodOrigen,
                   TieneComprobante = !string.IsNullOrWhiteSpace(gasto.RutaComprobante)
               };
    }

    private Task<Gasto?> ObtenerGastoParaComprobanteAsync(
        long idGasto,
        Guid idUsuario,
        CancellationToken cancellationToken) =>
        dbContext.Gastos.SingleOrDefaultAsync(
            gasto => gasto.IdGasto == idGasto && gasto.IdUsuario == idUsuario,
            cancellationToken);

    private async Task ValidarCategoriaYMedioPagoAsync(
        int idCategoriaGasto,
        int? idMedioPago,
        CancellationToken cancellationToken)
    {
        var categoriaActiva = await dbContext.CategoriasGasto.AsNoTracking().AnyAsync(
            categoria => categoria.IdCategoriaGasto == idCategoriaGasto && categoria.EstaActiva,
            cancellationToken);

        if (!categoriaActiva)
        {
            throw new GastoValidationException("La categoría seleccionada no existe o no está activa.");
        }

        if (!idMedioPago.HasValue)
        {
            return;
        }

        var medioPagoActivo = await dbContext.MediosPago.AsNoTracking().AnyAsync(
            medioPago => medioPago.IdMedioPago == idMedioPago.Value && medioPago.EstaActivo,
            cancellationToken);

        if (!medioPagoActivo)
        {
            throw new GastoValidationException("El medio de pago seleccionado no existe o no está activo.");
        }
    }

}
