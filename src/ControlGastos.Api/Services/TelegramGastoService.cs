using ControlGastos.Core.Entities;
using ControlGastos.Core.Interfaces;
using ControlGastos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlGastos.Api.Services;

public sealed class TelegramGastoService(
    ControlGastosDbContext db,
    ITelegramGastoParser parser,
    ICategorizadorGastoTelegramService categorizador) : ITelegramGastoService
{
    private const string Pendiente = "PENDIENTE";
    private const string Confirmado = "CONFIRMADO";
    private const string Cancelado = "CANCELADO";

    public async Task<TelegramGastoPropuestaResult> CrearPropuestaAsync(long chatId, string texto, CancellationToken ct)
    {
        var parseo = parser.Parse(texto);
        if (!parseo.EsValido)
        {
            return new TelegramGastoPropuestaResult(TelegramGastoPropuestaEstado.FormatoInvalido);
        }

        var usuario = await ObtenerUsuarioVinculadoAsync(chatId, ct);
        if (usuario is null)
        {
            return new TelegramGastoPropuestaResult(TelegramGastoPropuestaEstado.NoVinculado);
        }

        var nombreCategoria = categorizador.SugerirCategoria(parseo.Descripcion!);
        var categoria = await ObtenerCategoriaActivaAsync(nombreCategoria, ct);
        if (categoria is null)
        {
            return new TelegramGastoPropuestaResult(TelegramGastoPropuestaEstado.CategoriaNoDisponible);
        }

        var ahora = DateTimeOffset.UtcNow;
        var pendiente = new GastoPendienteTelegram
        {
            IdUsuario = usuario.IdUsuario,
            IdChatTelegram = chatId,
            Monto = parseo.Monto,
            Descripcion = parseo.Descripcion!,
            IdCategoriaGasto = categoria.IdCategoriaGasto,
            FechaGasto = ahora,
            CodEstado = Pendiente,
            FechaExpiracion = ahora.AddMinutes(10),
            FechaRegistro = ahora
        };

        db.GastosPendientesTelegram.Add(pendiente);
        await db.SaveChangesAsync(ct);

        return new TelegramGastoPropuestaResult(
            TelegramGastoPropuestaEstado.Creada,
            pendiente.IdGastoPendiente,
            pendiente.Monto,
            pendiente.Descripcion,
            categoria.Nombre);
    }

    public async Task<TelegramGastoCallbackResult> ConfirmarAsync(long idGastoPendiente, long chatId, long telegramUserId, CancellationToken ct)
    {
        var usuario = await ObtenerUsuarioVinculadoAsync(chatId, telegramUserId, ct);
        if (usuario is null)
        {
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.NoEncontrado);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var pendiente = await ObtenerPendienteBloqueadoAsync(idGastoPendiente, usuario.IdUsuario, chatId, ct);
        if (pendiente is null)
        {
            await transaction.RollbackAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.NoEncontrado);
        }

        if (pendiente.CodEstado == Confirmado)
        {
            await transaction.CommitAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.YaConfirmado);
        }

        if (pendiente.CodEstado == Cancelado)
        {
            await transaction.CommitAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.YaCancelado);
        }

        if (pendiente.FechaExpiracion <= DateTimeOffset.UtcNow)
        {
            pendiente.CodEstado = Cancelado;
            pendiente.FechaActualizacion = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.Expirado);
        }

        var categoria = await db.CategoriasGasto.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdCategoriaGasto == pendiente.IdCategoriaGasto, ct);
        if (categoria is null)
        {
            await transaction.RollbackAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.NoEncontrado);
        }

        db.Gastos.Add(new Gasto
        {
            IdUsuario = pendiente.IdUsuario,
            IdCategoriaGasto = pendiente.IdCategoriaGasto,
            IdMedioPago = null,
            CodEstado = Confirmado,
            Monto = pendiente.Monto,
            FechaGasto = pendiente.FechaGasto,
            Descripcion = pendiente.Descripcion,
            CodOrigen = "TELEGRAM",
            FechaRegistro = DateTimeOffset.UtcNow
        });
        pendiente.CodEstado = Confirmado;
        pendiente.FechaActualizacion = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new TelegramGastoCallbackResult(
            TelegramGastoCallbackEstado.Confirmado,
            pendiente.Monto,
            categoria.Nombre,
            pendiente.IdUsuario,
            pendiente.FechaGasto);
    }

    public async Task<TelegramGastoCallbackResult> CancelarAsync(long idGastoPendiente, long chatId, long telegramUserId, CancellationToken ct)
    {
        var usuario = await ObtenerUsuarioVinculadoAsync(chatId, telegramUserId, ct);
        if (usuario is null)
        {
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.NoEncontrado);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var pendiente = await ObtenerPendienteBloqueadoAsync(idGastoPendiente, usuario.IdUsuario, chatId, ct);
        if (pendiente is null)
        {
            await transaction.RollbackAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.NoEncontrado);
        }

        if (pendiente.CodEstado == Confirmado)
        {
            await transaction.CommitAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.YaConfirmado);
        }

        if (pendiente.CodEstado == Cancelado)
        {
            await transaction.CommitAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.YaCancelado);
        }

        pendiente.CodEstado = Cancelado;
        pendiente.FechaActualizacion = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.Cancelado);
    }

    private Task<UsuarioTelegram?> ObtenerUsuarioVinculadoAsync(long chatId, CancellationToken ct) =>
        db.UsuariosTelegram.SingleOrDefaultAsync(x => x.IdChatTelegram == chatId && x.EstaVinculado, ct);

    private Task<UsuarioTelegram?> ObtenerUsuarioVinculadoAsync(long chatId, long telegramUserId, CancellationToken ct) =>
        db.UsuariosTelegram.SingleOrDefaultAsync(
            x => x.IdChatTelegram == chatId && x.IdTelegram == telegramUserId && x.EstaVinculado,
            ct);

    private async Task<CategoriaGasto?> ObtenerCategoriaActivaAsync(string nombreSugerido, CancellationToken ct)
    {
        var categorias = await db.CategoriasGasto.Where(x => x.EstaActiva).ToListAsync(ct);
        var nombreNormalizado = CategorizadorGastoTelegramService.Normalizar(nombreSugerido);
        return categorias.SingleOrDefault(x => CategorizadorGastoTelegramService.Normalizar(x.Nombre) == nombreNormalizado);
    }

    private Task<GastoPendienteTelegram?> ObtenerPendienteBloqueadoAsync(long id, Guid idUsuario, long chatId, CancellationToken ct) =>
        db.GastosPendientesTelegram
            .FromSqlInterpolated($"select * from finanzas.tbt_gasto_pendiente_telegram where idd_gasto_pendiente = {id} and idd_usuario = {idUsuario} and idd_chat_telegram = {chatId} for update")
            .SingleOrDefaultAsync(ct);
}
