using ControlGastos.Core.DTOs;
using ControlGastos.Core.Entities;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Repositories;

public sealed class PerfilRepository(ControlGastosDbContext dbContext) : IPerfilRepository
{
    public Task<PerfilFinanciero_DTO?> ObtenerAsync(Guid idUsuario, CancellationToken cancellationToken) =>
        dbContext.Perfiles.AsNoTracking()
            .Where(perfil => perfil.IdUsuario == idUsuario && perfil.EstadoPerfil)
            .Select(perfil => new PerfilFinanciero_DTO
            {
                NombreUsuario = perfil.NombreUsuario,
                CodigoMoneda = perfil.CodigoMoneda,
                ZonaHoraria = perfil.ZonaHoraria,
                IngresoMensual = perfil.IngresoMensual,
                PresupuestoMensual = perfil.PresupuestoMensual,
                PorcentajeAlerta = perfil.PorcentajeAlerta
            })
            .SingleOrDefaultAsync(cancellationToken);

    public async Task GuardarAsync(Guid idUsuario, PerfilFinanciero_Actualizar_DTO dto, CancellationToken cancellationToken)
    {
        var perfil = await dbContext.Perfiles.SingleOrDefaultAsync(x => x.IdUsuario == idUsuario, cancellationToken);
        if (perfil is null)
        {
            dbContext.Perfiles.Add(new Perfil
            {
                IdUsuario = idUsuario,
                NombreUsuario = dto.NombreUsuario,
                CodigoMoneda = "PEN",
                ZonaHoraria = "America/Lima",
                IngresoMensual = dto.IngresoMensual,
                PresupuestoMensual = dto.PresupuestoMensual,
                PorcentajeAlerta = dto.PorcentajeAlerta,
                EstadoPerfil = true,
                FechaRegistro = DateTimeOffset.UtcNow
            });
        }
        else
        {
            perfil.NombreUsuario = dto.NombreUsuario;
            perfil.IngresoMensual = dto.IngresoMensual;
            perfil.PresupuestoMensual = dto.PresupuestoMensual;
            perfil.PorcentajeAlerta = dto.PorcentajeAlerta;
            perfil.FechaActualizacion = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
