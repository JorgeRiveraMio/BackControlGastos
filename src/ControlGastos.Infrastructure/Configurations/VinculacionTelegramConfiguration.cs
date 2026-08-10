using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class VinculacionTelegramConfiguration : IEntityTypeConfiguration<VinculacionTelegram>
{
    public void Configure(EntityTypeBuilder<VinculacionTelegram> builder)
    {
        builder.ToTable("tbt_vinculacion_telegram", "finanzas");

        builder.HasKey(x => x.IdVinculacion);

        builder.Property(x => x.IdVinculacion)
            .HasColumnName("idd_vinculacion")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdUsuario).HasColumnName("idd_usuario");
        builder.Property(x => x.CodigoToken).HasColumnName("cod_token").HasMaxLength(100);
        builder.Property(x => x.FechaExpiracion).HasColumnName("fec_expiracion");
        builder.Property(x => x.EstaUsado).HasColumnName("est_usado");
        builder.Property(x => x.FechaRegistro).HasColumnName("fec_regis");
    }
}
