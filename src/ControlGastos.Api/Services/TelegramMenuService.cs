using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Api.Services;

public sealed class TelegramMenuService(ControlGastosDbContext db)
{
    public async Task<object> CrearMenuCategoriasAsync(long idPendiente, CancellationToken ct)
    {
        var categorias = await db.CategoriasGasto.AsNoTracking().Where(x => x.EstaActiva)
            .OrderBy(x => x.Nombre).Select(x => new { x.IdCategoriaGasto, x.Nombre }).ToListAsync(ct);
        return CrearTeclado(categorias.Select(x => new Boton(x.Nombre, $"gasto_cat:{idPendiente}:{x.IdCategoriaGasto}")), idPendiente);
    }

    public async Task<object> CrearMenuMediosPagoAsync(long idPendiente, CancellationToken ct)
    {
        var medios = await db.MediosPago.AsNoTracking().Where(x => x.EstaActivo)
            .OrderBy(x => x.Nombre).Select(x => new { x.IdMedioPago, x.Nombre }).ToListAsync(ct);
        return CrearTeclado(medios.Select(x => new Boton(x.Nombre, $"gasto_pago:{idPendiente}:{x.IdMedioPago}")), idPendiente);
    }

    public static object CrearMenuPrincipal(long idPendiente) => new
    {
        inline_keyboard = new object[][]
        {
            [new { text = "✅ Confirmar", callback_data = $"gasto_ok:{idPendiente}" }, new { text = "🏷 Cambiar categoría", callback_data = $"gasto_catmenu:{idPendiente}" }],
            [new { text = "💳 Medio de pago", callback_data = $"gasto_pagomenu:{idPendiente}" }, new { text = "❌ Cancelar", callback_data = $"gasto_cancel:{idPendiente}" }]
        }
    };

    public static string FormatearPropuesta(TelegramGastoEdicionResult gasto) =>
        $"Gasto detectado 🧾\n\nMonto: S/ {gasto.Monto:N2}\nDescripción: {gasto.Descripcion}\nCategoría: {gasto.Categoria}\nMedio de pago: {gasto.MedioPago ?? "No indicado"}\n\n¿Deseas registrarlo?";

    private static object CrearTeclado(IEnumerable<Boton> botones, long idPendiente)
    {
        var filas = botones.Select(x => (object)new { text = x.Text, callback_data = x.Data })
            .Chunk(2).Select(x => x.ToArray()).ToList();
        filas.Add([new { text = "⬅ Volver", callback_data = $"gasto_volver:{idPendiente}" }]);
        return new { inline_keyboard = filas };
    }

    private sealed record Boton(string Text, string Data);
}
