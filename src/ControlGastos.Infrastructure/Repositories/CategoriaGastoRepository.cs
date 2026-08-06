using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Repositories;

public sealed class CategoriaGastoRepository(ControlGastosDbContext dbContext) : ICategoriaGastoRepository
{
    public async Task<IReadOnlyList<CategoriaGasto_Listar_DTO>> ObtenerActivasAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.CategoriasGasto
            .AsNoTracking()
            .Where(categoria => categoria.EstaActiva)
            .OrderBy(categoria => categoria.Nombre)
            .Select(categoria => new CategoriaGasto_Listar_DTO(
                categoria.IdCategoriaGasto,
                categoria.Nombre))
            .ToListAsync(cancellationToken);
    }
}
