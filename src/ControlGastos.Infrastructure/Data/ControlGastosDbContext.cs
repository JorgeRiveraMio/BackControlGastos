using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Infrastructure.Data;

public sealed class ControlGastosDbContext(
    DbContextOptions<ControlGastosDbContext> options) : DbContext(options)
{
    public DbSet<CategoriaGasto> CategoriasGasto => Set<CategoriaGasto>();

    public DbSet<MedioPago> MediosPago => Set<MedioPago>();

    public DbSet<EstadoGasto> EstadosGasto => Set<EstadoGasto>();

    public DbSet<Perfil> Perfiles => Set<Perfil>();

    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<UsuarioTelegram> UsuariosTelegram => Set<UsuarioTelegram>();
    public DbSet<VinculacionTelegram> VinculacionesTelegram => Set<VinculacionTelegram>();

    public DbSet<Gasto> Gastos => Set<Gasto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlGastosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
