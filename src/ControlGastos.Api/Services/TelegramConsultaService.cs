using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Api.Services;

public sealed class TelegramConsultaService(ControlGastosDbContext db, IDashboardRepository dashboard)
{
    public async Task<string> UltimoAsync(long chatId, CancellationToken ct)
    {
        var usuario = await UsuarioAsync(chatId, ct); if (usuario is null) return NoVinculado;
        var vinculo = usuario.Value;
        var gasto = await (from g in db.Gastos.AsNoTracking()
                           join c in db.CategoriasGasto.AsNoTracking() on g.IdCategoriaGasto equals c.IdCategoriaGasto
                           join m in db.MediosPago.AsNoTracking() on g.IdMedioPago equals m.IdMedioPago into medios
                           from m in medios.DefaultIfEmpty()
                           where g.IdUsuario == vinculo.Id && g.CodEstado != "ANULADO"
                           orderby g.FechaGasto descending
                           select new { g.Monto, g.Descripcion, Categoria = c.Nombre, Medio = m == null ? null : m.Nombre, g.FechaGasto }).FirstOrDefaultAsync(ct);
        if (gasto is null) return "Todavía no tienes gastos registrados.";
        var local = TimeZoneInfo.ConvertTime(gasto.FechaGasto, vinculo.Zona);
        var fecha = local.Date == TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, vinculo.Zona).Date ? $"Hoy {local:HH:mm}" : local.ToString("dd/MM HH:mm");
        return $"Último gasto 🧾\n\nS/ {gasto.Monto:N2}\n{Emoji(gasto.Categoria)} {gasto.Categoria}\n{gasto.Descripcion}\n{fecha}" + (gasto.Medio is null ? string.Empty : $"\nPago: {gasto.Medio}");
    }

    public async Task<string> HoyAsync(long chatId, CancellationToken ct)
    {
        var usuario = await UsuarioAsync(chatId, ct); if (usuario is null) return NoVinculado;
        var vinculo = usuario.Value;
        var localAhora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, vinculo.Zona);
        var inicio = AUTC(localAhora.Date, vinculo.Zona); var fin = AUTC(localAhora.Date.AddDays(1), vinculo.Zona);
        var filas = await (from g in db.Gastos.AsNoTracking() join c in db.CategoriasGasto.AsNoTracking() on g.IdCategoriaGasto equals c.IdCategoriaGasto
                           where g.IdUsuario == vinculo.Id && g.CodEstado != "ANULADO" && g.FechaGasto >= inicio && g.FechaGasto < fin
                           group g by c.Nombre into grupo orderby grupo.Sum(x => x.Monto) descending select new { Nombre = grupo.Key, Total = grupo.Sum(x => x.Monto), Cantidad = grupo.Count() }).ToListAsync(ct);
        if (filas.Count == 0) return "Hoy todavía no tienes gastos registrados.";
        return $"Hoy has gastado S/ {filas.Sum(x => x.Total):N2}\n\n" + string.Join("\n", filas.Select(x => $"{Emoji(x.Nombre)} {x.Nombre}: S/ {x.Total:N2}")) + $"\n\n{filas.Sum(x => x.Cantidad)} gastos registrados.";
    }

    public async Task<string> MesAsync(long chatId, CancellationToken ct)
    {
        var usuario = await UsuarioAsync(chatId, ct); if (usuario is null) return NoVinculado;
        var vinculo = usuario.Value;
        var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, vinculo.Zona);
        var resumen = await dashboard.ObtenerResumenMensualAsync(vinculo.Id, ahora.Year, ahora.Month, ct);
        if (resumen.CantidadGastos == 0) return "Este mes todavía no tienes gastos registrados.";
        var presupuesto = resumen.PresupuestoMensual is > 0 ? $"S/ {resumen.PresupuestoMensual:N2}" : "No configurado";
        var disponible = resumen.PresupuestoMensual is > 0 ? $"\nDisponible: S/ {(resumen.PresupuestoMensual.Value - resumen.TotalGastado):N2}\nUso: {resumen.PorcentajePresupuestoConsumido:N2}%" : string.Empty;
        return $"{ahora:MMMM yyyy} 📊\n\nGastado: S/ {resumen.TotalGastado:N2}\nPresupuesto: {presupuesto}{disponible}\n\nMayor categoría:\n{Emoji(resumen.CategoriaMayorGasto)} {resumen.CategoriaMayorGasto} · S/ {resumen.MontoCategoriaMayorGasto:N2}";
    }

    private async Task<(Guid Id, TimeZoneInfo Zona)?> UsuarioAsync(long chatId, CancellationToken ct)
    {
        var data = await (from u in db.UsuariosTelegram.AsNoTracking() join p in db.Perfiles.AsNoTracking() on u.IdUsuario equals p.IdUsuario
                          where u.IdChatTelegram == chatId && u.EstaVinculado && p.EstadoPerfil select new { u.IdUsuario, p.ZonaHoraria }).SingleOrDefaultAsync(ct);
        if (data is null) return null;
        try { return (data.IdUsuario, TimeZoneInfo.FindSystemTimeZoneById(data.ZonaHoraria)); } catch (TimeZoneNotFoundException) { return (data.IdUsuario, TimeZoneInfo.Utc); }
    }
    private static DateTimeOffset AUTC(DateTime local, TimeZoneInfo zona) => new(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zona));
    private static string Emoji(string? nombre) => nombre?.ToLowerInvariant() switch { "alimentación" or "alimentacion" => "🍽", "transporte" => "🚕", "entretenimiento" => "🎬", "servicios" => "💡", "compras" => "🛍", "salud" => "💊", "educación" or "educacion" => "📚", "vivienda" => "🏠", _ => "📌" };
    private const string NoVinculado = "Primero vincula tu cuenta de Telegram desde tu perfil de ControlGastos.";
}
