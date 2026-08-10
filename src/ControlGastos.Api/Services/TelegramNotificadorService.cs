using ControlGastos.Core;
using ControlGastos.Core.Entities;
using ControlGastos.Core.Interfaces;

namespace ControlGastos.Api.Services;

public sealed class TelegramNotificadorService(
    IUsuarioTelegramRepository usuarios,
    ITelegramBotClientService bot,
    ILogger<TelegramNotificadorService> logger) : ITelegramNotificadorService
{
    public async Task NotificarAlertaAsync(Guid id, Alerta alerta, CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerAsync(id, ct);
        if (usuario is null)
        {
            return;
        }

        var titulo = alerta.CodigoTipoAlerta == AlertaTipos.PresupuestoSuperado
            ? "🚨 Presupuesto superado"
            : "⚠️ Control de Gastos";
        var texto = $"{titulo}\n\n{alerta.DescripcionAlerta}\n\n" +
                    $"Presupuesto: S/ {alerta.MontoPresupuesto:N2}\n" +
                    $"Gastado: S/ {alerta.MontoGastado:N2}";

        try
        {
            await bot.EnviarMensajeAsync(usuario.IdChatTelegram, texto, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "No se pudo enviar una alerta de Telegram para el usuario {Usuario}. Tipo de error: {ExceptionType}.",
                id,
                exception.GetType().Name);
        }
    }
}
