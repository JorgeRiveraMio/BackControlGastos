using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Web.Controllers;

[SessionAuthorize]
public sealed class AlertasController(IControlGastosApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var resultado = await apiClient.ObtenerAlertasAsync(Token(), cancellationToken);
        if (Expirada(resultado)) return Login();
        if (!resultado.IsSuccess) TempData["Error"] = resultado.ErrorMessage ?? "No fue posible obtener las alertas.";
        return View(resultado.Data ?? []);
    }
    [HttpPost("Alertas/{id:long}/Leida")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(long id, CancellationToken cancellationToken)
    {
        var resultado = await apiClient.MarcarAlertaLeidaAsync(Token(), id, cancellationToken);
        if (Expirada(resultado)) return Login();
        if (!resultado.IsSuccess) TempData["Error"] = resultado.ErrorMessage ?? "No fue posible marcar la alerta.";
        return RedirectToAction(nameof(Index));
    }
    private string Token() => HttpContext.Session.GetString(SessionKeys.AccessToken)!;
    private bool Expirada<T>(ApiClientResult<T> r) { if (!r.IsUnauthorized) return false; HttpContext.Session.Clear(); return true; }
    private IActionResult Login() => RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index)) });
}
