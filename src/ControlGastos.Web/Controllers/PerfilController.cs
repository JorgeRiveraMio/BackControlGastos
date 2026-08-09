using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Models.ViewModels;
using ControlGastos.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Web.Controllers;

[SessionAuthorize]
public sealed class PerfilController(IControlGastosApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var resultado = await apiClient.ObtenerPerfilAsync(Token(), cancellationToken);
        var telegram = await apiClient.ObtenerEstadoTelegramAsync(Token(), cancellationToken);
        if (SesionExpirada(resultado)) return Login();
        if (!resultado.IsSuccess && resultado.Data is null)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "No fue posible obtener el perfil.";
        }

        var perfil = resultado.Data;
        return View(new PerfilFinancieroViewModel
        {
            NombreUsuario = perfil?.NombreUsuario ?? string.Empty,
            IngresoMensual = perfil?.IngresoMensual,
            PresupuestoMensual = perfil?.PresupuestoMensual,
            PorcentajeAlerta = perfil?.PorcentajeAlerta ?? 80
            ,TelegramVinculado = telegram.Data?.Vinculado == true, TelegramUsername = telegram.Data?.Username,
            TelegramEnlace = TempData["TelegramEnlace"] as string
        });
    }

    [HttpPost("Perfil/Telegram/Vinculacion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarVinculacionTelegram(CancellationToken cancellationToken)
    {
        var resultado = await apiClient.GenerarVinculacionTelegramAsync(Token(), cancellationToken);
        if (SesionExpirada(resultado)) return Login();
        if (!resultado.IsSuccess || resultado.Data is null) { TempData["Error"] = resultado.ErrorMessage ?? "No fue posible generar el enlace."; return RedirectToAction(nameof(Index)); }
        TempData["TelegramEnlace"] = $"https://t.me/{resultado.Data.BotUsername}?start={resultado.Data.Token}";
        TempData["Success"] = "Abre Telegram para completar la vinculación. El enlace vence en 10 minutos.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Perfil/Telegram/Desvincular")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesvincularTelegram(CancellationToken cancellationToken)
    { var resultado=await apiClient.DesvincularTelegramAsync(Token(),cancellationToken); if(SesionExpirada(resultado))return Login(); TempData[resultado.IsSuccess?"Success":"Error"]=resultado.IsSuccess?"Telegram desvinculado correctamente.":resultado.ErrorMessage??"No fue posible desvincular Telegram.";return RedirectToAction(nameof(Index)); }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PerfilFinancieroViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(modelo);
        var resultado = await apiClient.GuardarPerfilAsync(Token(), new PerfilFinancieroActualizarApiModel
        {
            NombreUsuario = modelo.NombreUsuario,
            IngresoMensual = modelo.IngresoMensual,
            PresupuestoMensual = modelo.PresupuestoMensual,
            PorcentajeAlerta = modelo.PorcentajeAlerta
        }, cancellationToken);
        if (SesionExpirada(resultado)) return Login();
        if (!resultado.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "No fue posible guardar el perfil.");
            return View(modelo);
        }
        TempData["Success"] = "Perfil guardado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private string Token() => HttpContext.Session.GetString(SessionKeys.AccessToken)!;
    private bool SesionExpirada<T>(ApiClientResult<T> resultado) { if (!resultado.IsUnauthorized) return false; HttpContext.Session.Clear(); return true; }
    private IActionResult Login() => RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index)) });
}
