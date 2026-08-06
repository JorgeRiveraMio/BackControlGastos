using ControlGastos.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlGastos.Infrastructure.Configurations;

public sealed class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("tbm_perfil", "finanzas");
        builder.HasKey(perfil => perfil.IdUsuario);

        builder.Property(perfil => perfil.IdUsuario).HasColumnName("idd_usuario");
        builder.Property(perfil => perfil.NombreUsuario).HasColumnName("nom_usuario").HasMaxLength(120).IsRequired();
        builder.Property(perfil => perfil.CodigoMoneda).HasColumnName("cod_moneda").HasMaxLength(3).IsRequired();
        builder.Property(perfil => perfil.ZonaHoraria).HasColumnName("zon_horaria").HasMaxLength(50).IsRequired();
        builder.Property(perfil => perfil.IngresoMensual).HasColumnName("mon_ingreso_mensual").HasPrecision(12, 2).IsRequired();
        builder.Property(perfil => perfil.PresupuestoMensual).HasColumnName("mon_presupuesto_mensual").HasPrecision(12, 2).IsRequired();
        builder.Property(perfil => perfil.PorcentajeAlerta).HasColumnName("por_alerta").HasPrecision(5, 2).IsRequired();
        builder.Property(perfil => perfil.EstaActivo).HasColumnName("est_perfil").IsRequired();
        builder.Property(perfil => perfil.FechaRegistro).HasColumnName("fec_regis").IsRequired();
        builder.Property(perfil => perfil.FechaActualizacion).HasColumnName("fec_actualizacion").IsRequired();
    }
}
