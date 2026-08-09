using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class AlertaConfiguration : IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("tbt_alerta", "finanzas");
        builder.HasKey(x => x.IdAlerta);
        builder.Property(x => x.IdAlerta).HasColumnName("idd_alerta");
        builder.Property(x => x.IdUsuario).HasColumnName("idd_usuario");
        builder.Property(x => x.CodigoTipoAlerta).HasColumnName("cod_tipo_alerta").HasMaxLength(30).IsRequired();
        builder.Property(x => x.DescripcionAlerta).HasColumnName("des_alerta").HasMaxLength(300).IsRequired();
        builder.Property(x => x.PorcentajeUmbral).HasColumnName("por_umbral").HasPrecision(5, 2);
        builder.Property(x => x.MontoPresupuesto).HasColumnName("mon_presupuesto").HasPrecision(12, 2);
        builder.Property(x => x.MontoGastado).HasColumnName("mon_gastado").HasPrecision(12, 2);
        builder.Property(x => x.AnioPeriodo).HasColumnName("anio_periodo");
        builder.Property(x => x.MesPeriodo).HasColumnName("mes_periodo");
        builder.Property(x => x.EstadoLeida).HasColumnName("est_leida").IsRequired();
        builder.Property(x => x.FechaRegistro).HasColumnName("fec_regis").IsRequired();
    }
}
