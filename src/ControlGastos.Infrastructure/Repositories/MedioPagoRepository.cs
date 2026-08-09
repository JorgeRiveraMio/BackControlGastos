using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Repositories;

public sealed class MedioPagoRepository(ControlGastosDbContext dbContext) : IMedioPagoRepository
{
    public async Task<IReadOnlyList<MedioPago_Listar_DTO>> ObtenerActivosAsync(CancellationToken cancellationToken)
    {
        return await dbContext.MediosPago
            .AsNoTracking()
            .Where(medioPago => medioPago.EstaActivo)
            .OrderBy(medioPago => medioPago.Nombre)
            .Select(medioPago => new MedioPago_Listar_DTO(medioPago.IdMedioPago, medioPago.Nombre))
            .ToListAsync(cancellationToken);
    }
}
