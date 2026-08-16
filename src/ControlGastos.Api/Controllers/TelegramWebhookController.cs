using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using ControlGastos.Api.Services;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Route("api/telegram/webhook")]
public sealed class TelegramWebhookController(
    IVinculacionTelegramRepository tokens,
    IUsuarioTelegramRepository usuarios,
    ITelegramBotClientService bot,
    ITelegramGastoService gastosTelegram,
    TelegramMenuService menus,
    TelegramConsultaService consultas,
    IEvaluadorPresupuestoService evaluadorPresupuestoService,
    IOptions<TelegramOptions> options,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Recibir([FromBody] TelegramUpdate update, CancellationToken ct)
    {
        if (!EsWebhookValido())
        {
            return Unauthorized();
        }

        if (update.CallbackQuery is not null)
        {
            await ProcesarCallbackAsync(update.CallbackQuery, ct);
            return Ok();
        }

        if (update.Message is not null)
        {
            await ProcesarMensajeAsync(update.Message, ct);
        }

        return Ok();
    }

    private bool EsWebhookValido()
    {
        var configurado = options.Value.WebhookSecret;
        var recibido = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
        return !string.IsNullOrWhiteSpace(configurado) &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(configurado),
                   Encoding.UTF8.GetBytes(recibido));
    }

    private async Task ProcesarMensajeAsync(TelegramMessage mensaje, CancellationToken ct)
    {
        var texto = mensaje.Text?.Trim();
        var chat = mensaje.Chat;
        if (string.IsNullOrWhiteSpace(texto) || chat is null)
        {
            return;
        }

        if (texto.StartsWith("/start ", StringComparison.Ordinal))
        {
            await ProcesarInicioAsync(texto[7..].Trim(), mensaje, ct);
            return;
        }

        if (texto.Equals("/ayuda", StringComparison.OrdinalIgnoreCase))
        {
            await EnviarSinReintentoAsync(() => bot.EnviarMensajeAsync(chat.Id, "ControlGastos 🤖\n\nRegistrar gasto:\n18.50 almuerzo\n\nTambién puedes usar:\n/gasto 18.50 almuerzo\n\nConsultas:\n/ultimo - último gasto\n/hoy - resumen de hoy\n/mes - resumen del mes", ct), chat.Id);
            return;
        }
        if (texto.Equals("/ultimo", StringComparison.OrdinalIgnoreCase)) { await EnviarSinReintentoAsync(() => EnviarConsultaAsync(chat.Id, consultas.UltimoAsync(chat.Id, ct), ct), chat.Id); return; }
        if (texto.Equals("/hoy", StringComparison.OrdinalIgnoreCase)) { await EnviarSinReintentoAsync(() => EnviarConsultaAsync(chat.Id, consultas.HoyAsync(chat.Id, ct), ct), chat.Id); return; }
        if (texto.Equals("/mes", StringComparison.OrdinalIgnoreCase)) { await EnviarSinReintentoAsync(() => EnviarConsultaAsync(chat.Id, consultas.MesAsync(chat.Id, ct), ct), chat.Id); return; }

        var textoGasto = texto.StartsWith("/gasto ", StringComparison.OrdinalIgnoreCase)
            ? texto[7..].Trim()
            : texto;
        var propuesta = await gastosTelegram.CrearPropuestaAsync(chat.Id, textoGasto, ct);

        switch (propuesta.Estado)
        {
            case TelegramGastoPropuestaEstado.Creada:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeConBotonesAsync(
                        chat.Id,
                        $"Gasto detectado 🧾\n\nMonto: S/ {propuesta.Monto:N2}\nDescripción: {propuesta.Descripcion}\nCategoría: {propuesta.Categoria}\nMedio de pago: No indicado\n\n¿Deseas registrarlo?",
                        propuesta.IdGastoPendiente!.Value,
                        ct),
                    chat.Id);
                break;
            case TelegramGastoPropuestaEstado.NoVinculado:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "Primero vincula tu cuenta de Telegram desde tu perfil de ControlGastos.", ct),
                    chat.Id);
                break;
            case TelegramGastoPropuestaEstado.FormatoInvalido:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "No pude interpretar el gasto.\n\nPrueba escribiendo:\n18.50 almuerzo\no\n/gasto 18.50 almuerzo", ct),
                    chat.Id);
                break;
            case TelegramGastoPropuestaEstado.CategoriaNoDisponible:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "No encontré una categoría activa para ese gasto. Revisa tus categorías en ControlGastos.", ct),
                    chat.Id);
                break;
        }
    }

    private async Task ProcesarInicioAsync(string token, TelegramMessage mensaje, CancellationToken ct)
    {
        var chat = mensaje.Chat;
        var from = mensaje.From;
        var vinculacion = await tokens.ObtenerValidaAsync(token, ct);
        if (vinculacion is null || chat is null || from is null)
        {
            if (chat is not null)
            {
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "El enlace de vinculación no es válido o expiró.", ct),
                    chat.Id);
            }

            return;
        }

        await usuarios.VincularAsync(vinculacion.IdUsuario, from.Id, chat.Id, from.Username, ct);
        await tokens.MarcarUsadaAsync(vinculacion.IdVinculacion, ct);
        await EnviarSinReintentoAsync(
            () => bot.EnviarMensajeAsync(chat.Id, "Telegram quedó vinculado correctamente con tu cuenta de ControlGastos.", ct),
            chat.Id);
    }

    private async Task ProcesarCallbackAsync(TelegramCallbackQuery callback, CancellationToken ct)
    {
        var chat = callback.Message?.Chat;
        if (chat is null || callback.Message is null || !TryObtenerAccion(callback.Data, out var accion, out var idGastoPendiente, out var idCatalogo))
        {
            await EnviarCallbackSinReintentoAsync(callback.Id, ct);
            return;
        }

        await EnviarCallbackSinReintentoAsync(callback.Id, ct);

        if (accion is "catmenu" or "pagomenu")
        {
            var propuesta = await gastosTelegram.ObtenerPropuestaAsync(idGastoPendiente, chat.Id, callback.From.Id, ct);
            if (propuesta.Estado == TelegramGastoCallbackEstado.Confirmado)
            {
                var markup = accion == "catmenu" ? await menus.CrearMenuCategoriasAsync(idGastoPendiente, ct) : await menus.CrearMenuMediosPagoAsync(idGastoPendiente, ct);
                var texto = accion == "catmenu" ? "Selecciona una categoría:" : "¿Cómo pagaste?";
                await EnviarSinReintentoAsync(() => bot.EditarMensajeAsync(chat.Id, callback.Message.MessageId, texto, markup, ct), chat.Id);
            }
            else await EnviarResultadoEdicionAsync(chat.Id, propuesta.Estado, ct);
            return;
        }
        if (accion is "cat" or "pago" or "volver")
        {
            var propuesta = accion == "cat" ? await gastosTelegram.CambiarCategoriaAsync(idGastoPendiente, idCatalogo, chat.Id, callback.From.Id, ct)
                : accion == "pago" ? await gastosTelegram.CambiarMedioPagoAsync(idGastoPendiente, idCatalogo, chat.Id, callback.From.Id, ct)
                : await gastosTelegram.ObtenerPropuestaAsync(idGastoPendiente, chat.Id, callback.From.Id, ct);
            if (propuesta.Estado == TelegramGastoCallbackEstado.Confirmado)
                await EnviarSinReintentoAsync(() => bot.EditarMensajeAsync(chat.Id, callback.Message.MessageId, TelegramMenuService.FormatearPropuesta(propuesta), TelegramMenuService.CrearMenuPrincipal(idGastoPendiente), ct), chat.Id);
            else await EnviarResultadoEdicionAsync(chat.Id, propuesta.Estado, ct);
            return;
        }

        var resultado = accion == "ok" ? await gastosTelegram.ConfirmarAsync(idGastoPendiente, chat.Id, callback.From.Id, ct)
            : await gastosTelegram.CancelarAsync(idGastoPendiente, chat.Id, callback.From.Id, ct);

        if (resultado.Estado == TelegramGastoCallbackEstado.Confirmado && resultado.IdUsuario.HasValue && resultado.FechaGasto.HasValue)
        {
            try
            {
                await evaluadorPresupuestoService.EvaluarAsync(resultado.IdUsuario.Value, resultado.FechaGasto.Value, ct);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No se pudo evaluar el presupuesto después de registrar un gasto por Telegram.");
            }
        }

        var respuesta = resultado.Estado switch
        {
            TelegramGastoCallbackEstado.Confirmado => $"Gasto registrado correctamente ✅\nS/ {resultado.Monto:N2} · {resultado.Categoria}",
            TelegramGastoCallbackEstado.YaConfirmado => "Este gasto ya fue registrado correctamente.",
            TelegramGastoCallbackEstado.Cancelado or TelegramGastoCallbackEstado.YaCancelado => "Gasto cancelado.",
            TelegramGastoCallbackEstado.Expirado => "Esta solicitud expiró. Envía el gasto nuevamente.",
            _ => "No encontré una solicitud de gasto válida para este chat."
        };

        await EnviarSinReintentoAsync(() => bot.EnviarMensajeAsync(chat.Id, respuesta, ct), chat.Id);
    }

    private static bool TryObtenerAccion(string? data, out string accion, out long idGastoPendiente, out int idCatalogo)
    {
        accion = string.Empty;
        idGastoPendiente = 0;
        idCatalogo = 0;
        if (string.IsNullOrWhiteSpace(data)) return false;

        var partes = data.Split(':', StringSplitOptions.TrimEntries);
        if (partes.Length is not (2 or 3) || !partes[0].StartsWith("gasto_", StringComparison.Ordinal) || !long.TryParse(partes[1], out idGastoPendiente) || idGastoPendiente <= 0) return false;
        accion = partes[0][6..];
        if (accion is "cat" or "pago") return partes.Length == 3 && int.TryParse(partes[2], out idCatalogo) && idCatalogo > 0;
        return partes.Length == 2 && accion is "ok" or "cancel" or "catmenu" or "pagomenu" or "volver";
    }

    private Task EnviarConsultaAsync(long chatId, Task<string> consulta, CancellationToken ct) => EnviarConsultaInternaAsync(chatId, consulta, ct);
    private async Task EnviarConsultaInternaAsync(long chatId, Task<string> consulta, CancellationToken ct) => await bot.EnviarMensajeAsync(chatId, await consulta, ct);
    private async Task EnviarResultadoEdicionAsync(long chatId, TelegramGastoCallbackEstado estado, CancellationToken ct)
    {
        var texto = estado == TelegramGastoCallbackEstado.Expirado ? "Esta solicitud expiró ⏱\nEnvía el gasto nuevamente." : "No encontré una solicitud de gasto válida para este chat.";
        await EnviarSinReintentoAsync(() => bot.EnviarMensajeAsync(chatId, texto, ct), chatId);
    }

    private async Task EnviarCallbackSinReintentoAsync(string callbackId, CancellationToken ct) =>
        await EnviarSinReintentoAsync(() => bot.ResponderCallbackAsync(callbackId, ct), null);

    private async Task EnviarSinReintentoAsync(Func<Task> enviar, long? chatId)
    {
        try
        {
            await enviar();
        }
        catch (Exception exception)
        {
            logger.LogWarning("No se pudo enviar una respuesta de Telegram para el chat {ChatId}. Tipo de error: {ExceptionType}.", chatId, exception.GetType().Name);
        }
    }
}

public sealed class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }

    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; init; }

    [JsonPropertyName("callback_query")]
    public TelegramCallbackQuery? CallbackQuery { get; init; }
}

public sealed class TelegramCallbackQuery
{
    public string Id { get; init; } = string.Empty;
    public TelegramUser From { get; init; } = new();
    public TelegramMessage? Message { get; init; }
    public string? Data { get; init; }
}

public sealed class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; init; }
    public string? Text { get; init; }
    public TelegramChat? Chat { get; init; }
    public TelegramUser? From { get; init; }
}

public sealed class TelegramChat
{
    public long Id { get; init; }
}

public sealed class TelegramUser
{
    public long Id { get; init; }
    public string? Username { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }
}
