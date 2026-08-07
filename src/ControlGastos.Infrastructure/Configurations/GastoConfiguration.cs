using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class GastoConfiguration : IEntityTypeConfiguration<Gasto>
{
    public void Configure(EntityTypeBuilder<Gasto> builder)
    {
        builder.ToTable("tbm_gasto", "finanzas");
        builder.HasKey(gasto => gasto.IdGasto);

        builder.Property(gasto => gasto.IdGasto).HasColumnName("idd_gasto");
        builder.Property(gasto => gasto.IdUsuario).HasColumnName("idd_usuario");
        builder.Property(gasto => gasto.IdCategoriaGasto).HasColumnName("idd_categ_gasto");
        builder.Property(gasto => gasto.IdMedioPago).HasColumnName("idd_medio_pago");
        builder.Property(gasto => gasto.CodEstado).HasColumnName("cod_estado").HasMaxLength(20).IsRequired();
        builder.Property(gasto => gasto.Monto).HasColumnName("mon_gasto").HasPrecision(12, 2).IsRequired();
        builder.Property(gasto => gasto.FechaGasto).HasColumnName("fec_gasto").IsRequired();
        builder.Property(gasto => gasto.NombreComercio).HasColumnName("nom_comercio").HasMaxLength(150);
        builder.Property(gasto => gasto.Descripcion).HasColumnName("des_gasto").HasMaxLength(300);
        builder.Property(gasto => gasto.CodOrigen).HasColumnName("cod_origen").HasMaxLength(20).IsRequired();
        builder.Property(gasto => gasto.RutaComprobante).HasColumnName("rut_comprobante").HasMaxLength(500);
        builder.Property(gasto => gasto.NombreComprobante).HasColumnName("nom_comprobante").HasMaxLength(250);
        builder.Property(gasto => gasto.FechaRegistro).HasColumnName("fec_regis").IsRequired();
        builder.Property(gasto => gasto.FechaActualizacion).HasColumnName("fec_actualizacion");
    }
}
