using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class CategoriaGastoConfiguration : IEntityTypeConfiguration<CategoriaGasto>
{
    public void Configure(EntityTypeBuilder<CategoriaGasto> builder)
    {
        builder.ToTable("tbt_categ_gasto", "finanzas");
        builder.HasKey(categoria => categoria.IdCategoriaGasto);

        builder.Property(categoria => categoria.IdCategoriaGasto).HasColumnName("idd_categ_gasto");
        builder.Property(categoria => categoria.Nombre).HasColumnName("nom_categ_gasto").HasMaxLength(100).IsRequired();
        builder.Property(categoria => categoria.EstaActiva).HasColumnName("est_categ_gasto").IsRequired();
        builder.Property(categoria => categoria.FechaRegistro).HasColumnName("fec_regis").IsRequired();
    }
}
