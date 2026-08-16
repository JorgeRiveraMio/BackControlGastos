using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class GastoPendienteTelegramConfiguration : IEntityTypeConfiguration<GastoPendienteTelegram>
{
    public void Configure(EntityTypeBuilder<GastoPendienteTelegram> builder)
    {
        builder.ToTable("tbt_gasto_pendiente_telegram", "finanzas");
        builder.HasKey(x => x.IdGastoPendiente);
        builder.Property(x => x.IdGastoPendiente).HasColumnName("idd_gasto_pendiente").ValueGeneratedOnAdd();
        builder.Property(x => x.IdUsuario).HasColumnName("idd_usuario");
        builder.Property(x => x.IdChatTelegram).HasColumnName("idd_chat_telegram");
        builder.Property(x => x.Monto).HasColumnName("mon_gasto").HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("des_gasto").HasMaxLength(250).IsRequired();
        builder.Property(x => x.IdCategoriaGasto).HasColumnName("idd_categ_gasto");
        builder.Property(x => x.FechaGasto).HasColumnName("fec_gasto").IsRequired();
        builder.Property(x => x.CodEstado).HasColumnName("cod_estado").HasMaxLength(20).IsRequired();
        builder.Property(x => x.FechaExpiracion).HasColumnName("fec_expiracion").IsRequired();
        builder.Property(x => x.FechaRegistro).HasColumnName("fec_regis").IsRequired();
        builder.Property(x => x.FechaActualizacion).HasColumnName("fec_actualizacion");
        builder.HasIndex(x => x.IdChatTelegram);
        builder.HasIndex(x => x.IdUsuario);
        builder.HasIndex(x => x.CodEstado);
        builder.HasOne<CategoriaGasto>().WithMany().HasForeignKey(x => x.IdCategoriaGasto).OnDelete(DeleteBehavior.Restrict);
    }
}
