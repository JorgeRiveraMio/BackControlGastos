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
            .SingleOrDefaultAsync(x => x.IdCategoriaGasto == pendiente.IdCategoriaGasto && x.EstaActiva, ct);
        if (categoria is null)
        {
            await transaction.RollbackAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.NoEncontrado);
        }

        if (pendiente.IdMedioPago.HasValue && !await db.MediosPago.AsNoTracking()
                .AnyAsync(x => x.IdMedioPago == pendiente.IdMedioPago.Value && x.EstaActivo, ct))
        {
            await transaction.RollbackAsync(ct);
            return new TelegramGastoCallbackResult(TelegramGastoCallbackEstado.NoEncontrado);
        }

        db.Gastos.Add(new Gasto
        {
            IdUsuario = pendiente.IdUsuario,
            IdCategoriaGasto = pendiente.IdCategoriaGasto,
            IdMedioPago = pendiente.IdMedioPago,
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

    public Task<TelegramGastoEdicionResult> ObtenerPropuestaAsync(long id, long chatId, long telegramUserId, CancellationToken ct) =>
        ObtenerEdicionAsync(id, chatId, telegramUserId, null, null, ct);

    public Task<TelegramGastoEdicionResult> CambiarCategoriaAsync(long id, int categoriaId, long chatId, long telegramUserId, CancellationToken ct) =>
        ObtenerEdicionAsync(id, chatId, telegramUserId, categoriaId, null, ct);

    public Task<TelegramGastoEdicionResult> CambiarMedioPagoAsync(long id, int medioPagoId, long chatId, long telegramUserId, CancellationToken ct) =>
        ObtenerEdicionAsync(id, chatId, telegramUserId, null, medioPagoId, ct);

    private async Task<TelegramGastoEdicionResult> ObtenerEdicionAsync(long id, long chatId, long telegramUserId, int? categoriaId, int? medioPagoId, CancellationToken ct)
    {
        var usuario = await ObtenerUsuarioVinculadoAsync(chatId, telegramUserId, ct);
        if (usuario is null) return new(TelegramGastoCallbackEstado.NoEncontrado);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var pendiente = await ObtenerPendienteBloqueadoAsync(id, usuario.IdUsuario, chatId, ct);
        if (pendiente is null) { await transaction.RollbackAsync(ct); return new(TelegramGastoCallbackEstado.NoEncontrado); }
        if (pendiente.CodEstado == Confirmado) { await transaction.CommitAsync(ct); return new(TelegramGastoCallbackEstado.YaConfirmado); }
        if (pendiente.CodEstado == Cancelado) { await transaction.CommitAsync(ct); return new(TelegramGastoCallbackEstado.YaCancelado); }
        if (pendiente.FechaExpiracion <= DateTimeOffset.UtcNow)
        {
            pendiente.CodEstado = Cancelado; pendiente.FechaActualizacion = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return new(TelegramGastoCallbackEstado.Expirado);
        }
        if (categoriaId.HasValue)
        {
            var categoriaActiva = await db.CategoriasGasto.AsNoTracking().AnyAsync(x => x.IdCategoriaGasto == categoriaId && x.EstaActiva, ct);
            if (!categoriaActiva) { await transaction.RollbackAsync(ct); return new(TelegramGastoCallbackEstado.NoEncontrado); }
            pendiente.IdCategoriaGasto = categoriaId.Value; pendiente.FechaActualizacion = DateTimeOffset.UtcNow;
        }
        if (medioPagoId.HasValue)
        {
            var medioActivo = await db.MediosPago.AsNoTracking().AnyAsync(x => x.IdMedioPago == medioPagoId && x.EstaActivo, ct);
            if (!medioActivo) { await transaction.RollbackAsync(ct); return new(TelegramGastoCallbackEstado.NoEncontrado); }
            pendiente.IdMedioPago = medioPagoId; pendiente.FechaActualizacion = DateTimeOffset.UtcNow;
        }
        if (categoriaId.HasValue || medioPagoId.HasValue) await db.SaveChangesAsync(ct);
        var detalle = await (from categoria in db.CategoriasGasto.AsNoTracking()
                             where categoria.IdCategoriaGasto == pendiente.IdCategoriaGasto
                             join medio in db.MediosPago.AsNoTracking() on pendiente.IdMedioPago equals medio.IdMedioPago into medios
                             from medio in medios.DefaultIfEmpty()
                             select new { categoria.Nombre, Medio = medio == null ? null : medio.Nombre }).SingleAsync(ct);
        await transaction.CommitAsync(ct);
        return new(TelegramGastoCallbackEstado.Confirmado, pendiente.IdGastoPendiente, pendiente.Monto, pendiente.Descripcion, detalle.Nombre, detalle.Medio);
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
