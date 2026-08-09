using ControlGastos.Core.DTOs;
using ControlGastos.Core.Entities;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Repositories;

public sealed class AlertaRepository(ControlGastosDbContext dbContext) : IAlertaRepository
{
    public Task<bool> ExisteAsync(Guid idUsuario, string codigoTipo, int anio, int mes, CancellationToken cancellationToken) =>
        dbContext.Alertas.AsNoTracking().AnyAsync(x => x.IdUsuario == idUsuario && x.CodigoTipoAlerta == codigoTipo && x.AnioPeriodo == anio && x.MesPeriodo == mes, cancellationToken);

    public async Task CrearAsync(Alerta alerta, CancellationToken cancellationToken)
    {
        dbContext.Alertas.Add(alerta);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alerta_Listar_DTO>> ListarAsync(Guid idUsuario, CancellationToken cancellationToken) =>
        await dbContext.Alertas.AsNoTracking().Where(x => x.IdUsuario == idUsuario).OrderByDescending(x => x.FechaRegistro)
            .Select(x => new Alerta_Listar_DTO { IdAlerta = x.IdAlerta, CodigoTipoAlerta = x.CodigoTipoAlerta, Mensaje = x.DescripcionAlerta, PorcentajeUmbral = x.PorcentajeUmbral, MontoPresupuesto = x.MontoPresupuesto, MontoGastado = x.MontoGastado, Anio = x.AnioPeriodo, Mes = x.MesPeriodo, Leida = x.EstadoLeida, FechaRegistro = x.FechaRegistro }).ToListAsync(cancellationToken);

    public async Task MarcarLeidaAsync(Guid idUsuario, long idAlerta, CancellationToken cancellationToken)
    {
        var alerta = await dbContext.Alertas.SingleOrDefaultAsync(x => x.IdAlerta == idAlerta && x.IdUsuario == idUsuario, cancellationToken);
        if (alerta is null || alerta.EstadoLeida) return;
        alerta.EstadoLeida = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> ContarNoLeidasAsync(Guid idUsuario, CancellationToken cancellationToken) =>
        dbContext.Alertas.AsNoTracking().CountAsync(x => x.IdUsuario == idUsuario && !x.EstadoLeida, cancellationToken);
}
