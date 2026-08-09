using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Repositories;

public sealed class DashboardRepository(ControlGastosDbContext dbContext) : IDashboardRepository
{
    public async Task<DashboardResumenMensual_DTO> ObtenerResumenMensualAsync(
        Guid idUsuario, int anio, int mes, CancellationToken cancellationToken)
    {
        var (inicioUtc, finUtc) = FechaLima.ObtenerLimitesMensualesUtc(anio, mes);
        var gastos = dbContext.Gastos.AsNoTracking()
            .Where(gasto => gasto.IdUsuario == idUsuario && gasto.CodEstado != "ANULADO"
                && gasto.FechaGasto >= inicioUtc && gasto.FechaGasto < finUtc);

        var perfil = await dbContext.Perfiles.AsNoTracking()
            .Where(perfil => perfil.IdUsuario == idUsuario && perfil.EstadoPerfil)
            .Select(perfil => new { perfil.IngresoMensual, perfil.PresupuestoMensual, perfil.PorcentajeAlerta })
            .SingleOrDefaultAsync(cancellationToken);
        var resumen = await gastos.GroupBy(_ => 1)
            .Select(grupo => new { Total = grupo.Sum(x => x.Monto), Cantidad = grupo.Count() })
            .SingleOrDefaultAsync(cancellationToken);
        var categorias = await (from gasto in gastos
                              join categoria in dbContext.CategoriasGasto.AsNoTracking()
                                  on gasto.IdCategoriaGasto equals categoria.IdCategoriaGasto
                              group gasto by categoria.Nombre into grupo
                              orderby grupo.Sum(x => x.Monto) descending
                              select new DashboardCategoria_DTO
                              {
                                  NombreCategoria = grupo.Key,
                                  Monto = grupo.Sum(x => x.Monto),
                                  Cantidad = grupo.Count()
                              }).ToListAsync(cancellationToken);
        var ultimos = await (from gasto in gastos
                           join categoria in dbContext.CategoriasGasto.AsNoTracking()
                               on gasto.IdCategoriaGasto equals categoria.IdCategoriaGasto
                           orderby gasto.FechaGasto descending
                           select new DashboardUltimoGasto_DTO
                           {
                               IdGasto = gasto.IdGasto,
                               FechaGasto = gasto.FechaGasto,
                               NombreCategoria = categoria.Nombre,
                               NombreComercio = gasto.NombreComercio,
                               Monto = gasto.Monto,
                               TieneComprobante = !string.IsNullOrWhiteSpace(gasto.RutaComprobante)
                           }).Take(5).ToListAsync(cancellationToken);
        var total = resumen?.Total ?? 0;
        var cantidad = resumen?.Cantidad ?? 0;
        var presupuesto = perfil?.PresupuestoMensual;

        return new DashboardResumenMensual_DTO
        {
            IngresoMensual = perfil?.IngresoMensual,
            PresupuestoMensual = presupuesto,
            PorcentajeAlerta = perfil?.PorcentajeAlerta ?? 80,
            TotalGastado = total,
            DisponibleEstimado = perfil?.IngresoMensual - total,
            PorcentajePresupuestoConsumido = presupuesto is > 0 ? total / presupuesto * 100 : null,
            CantidadGastos = cantidad,
            PromedioGasto = cantidad == 0 ? 0 : total / cantidad,
            CategoriaMayorGasto = categorias.FirstOrDefault()?.NombreCategoria,
            MontoCategoriaMayorGasto = categorias.FirstOrDefault()?.Monto ?? 0,
            GastosPorCategoria = categorias,
            UltimosGastos = ultimos
        };
    }
}
