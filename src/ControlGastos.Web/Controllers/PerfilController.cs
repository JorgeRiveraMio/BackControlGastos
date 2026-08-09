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
        });
    }

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
