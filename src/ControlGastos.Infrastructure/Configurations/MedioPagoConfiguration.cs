using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class MedioPagoConfiguration : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> builder)
    {
        builder.ToTable("tbt_medio_pago", "finanzas");
        builder.HasKey(medioPago => medioPago.IdMedioPago);

        builder.Property(medioPago => medioPago.IdMedioPago).HasColumnName("idd_medio_pago");
        builder.Property(medioPago => medioPago.Nombre).HasColumnName("nom_medio_pago").HasMaxLength(50).IsRequired();
        builder.Property(medioPago => medioPago.EstaActivo).HasColumnName("est_medio_pago").IsRequired();
        builder.Property(medioPago => medioPago.FechaRegistro).HasColumnName("fec_regis").IsRequired();
    }
}
