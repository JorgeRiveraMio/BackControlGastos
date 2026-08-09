using ControlGastos.Core;
using ControlGastos.Core.Entities;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ControlGastos.Infrastructure.Services;

public sealed class EvaluadorPresupuestoService(
    ControlGastosDbContext dbContext,
    IPerfilRepository perfilRepository,
    IAlertaRepository alertaRepository) : IEvaluadorPresupuestoService
{
    public async Task EvaluarAsync(Guid idUsuario, DateTimeOffset fechaGasto, CancellationToken cancellationToken)
    {
        var perfil = await perfilRepository.ObtenerAsync(idUsuario, cancellationToken);
        if (perfil?.PresupuestoMensual is not > 0) return;

        var fechaLima = TimeZoneInfo.ConvertTime(fechaGasto, ObtenerZonaLima());
        var anio = fechaLima.Year;
        var mes = fechaLima.Month;
        var (inicioUtc, finUtc) = FechaLima.ObtenerLimitesMensualesUtc(anio, mes);
        var total = await dbContext.Gastos.AsNoTracking()
            .Where(x => x.IdUsuario == idUsuario && x.CodEstado != "ANULADO" && x.FechaGasto >= inicioUtc && x.FechaGasto < finUtc)
            .SumAsync(x => (decimal?)x.Monto, cancellationToken) ?? 0;
        var porcentaje = total / perfil.PresupuestoMensual.Value * 100;
        if (porcentaje < perfil.PorcentajeAlerta) return;

        var tipo = porcentaje >= 100 ? AlertaTipos.PresupuestoSuperado : AlertaTipos.PresupuestoAdvertencia;
        var mensaje = tipo == AlertaTipos.PresupuestoSuperado
            ? "Has alcanzado o superado tu presupuesto mensual."
            : $"Has consumido el {perfil.PorcentajeAlerta:0.##}% de tu presupuesto mensual.";
        if (await alertaRepository.ExisteAsync(idUsuario, tipo, anio, mes, cancellationToken)) return;

        try
        {
            await alertaRepository.CrearAsync(new Alerta { IdUsuario = idUsuario, CodigoTipoAlerta = tipo, DescripcionAlerta = mensaje, PorcentajeUmbral = perfil.PorcentajeAlerta, MontoPresupuesto = perfil.PresupuestoMensual, MontoGastado = total, AnioPeriodo = anio, MesPeriodo = mes, EstadoLeida = false, FechaRegistro = DateTimeOffset.UtcNow }, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // La restricción única confirma que otra solicitud concurrente ya creó la alerta.
        }
    }

    private static TimeZoneInfo ObtenerZonaLima()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
    }
}
