using ControlGastos.Core.DTOs;
using ControlGastos.Core.Entities;
using ControlGastos.Core.Exceptions;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Repositories;

public sealed class GastoRepository(ControlGastosDbContext dbContext) : IGastoRepository
{
    private static readonly TimeZoneInfo ZonaHorariaLima = ObtenerZonaHorariaLima();

    public async Task<long> RegistrarAsync(Gasto_Registrar_DTO dto, CancellationToken cancellationToken)
    {
        await ValidarCategoriaYMedioPagoAsync(dto.IdCategoriaGasto, dto.IdMedioPago, cancellationToken);

        var gasto = new Gasto
        {
            IdUsuario = dto.IdUsuario,
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
        var inicioMesLima = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var fechaInicioUtc = ConvertirHoraLimaAUtc(inicioMesLima);
        var fechaFinUtc = ConvertirHoraLimaAUtc(inicioMesLima.AddMonths(1));

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
                   CodOrigen = gasto.CodOrigen
               };
    }

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

    private static DateTimeOffset ConvertirHoraLimaAUtc(DateTime fechaHoraLima)
    {
        var fechaHoraUtc = TimeZoneInfo.ConvertTimeToUtc(fechaHoraLima, ZonaHorariaLima);

        return new DateTimeOffset(fechaHoraUtc);
    }

    private static TimeZoneInfo ObtenerZonaHorariaLima()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }
}
