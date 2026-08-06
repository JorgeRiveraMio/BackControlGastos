using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class EstadoGastoConfiguration : IEntityTypeConfiguration<EstadoGasto>
{
    public void Configure(EntityTypeBuilder<EstadoGasto> builder)
    {
        builder.ToTable("tbt_estad_gasto", "finanzas");
        builder.HasKey(estado => estado.CodigoEstado);

        builder.Property(estado => estado.CodigoEstado).HasColumnName("cod_estado").HasMaxLength(20);
        builder.Property(estado => estado.Nombre).HasColumnName("nom_estado").HasMaxLength(50).IsRequired();
        builder.Property(estado => estado.EstaActivo).HasColumnName("est_estado").IsRequired();
    }
}
