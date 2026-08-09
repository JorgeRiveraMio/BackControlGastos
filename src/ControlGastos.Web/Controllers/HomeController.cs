using System.Diagnostics;
using ControlGastos.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ControlGastos.Web.Models;
using ControlGastos.Web.Models.ViewModels;
using ControlGastos.Web.Services;

namespace ControlGastos.Web.Controllers;

[SessionAuthorize]
public sealed class HomeController(IControlGastosApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accessToken = HttpContext.Session.GetString(SessionKeys.AccessToken)!;
        var resultado = await apiClient.ObtenerCategoriasAsync(accessToken, cancellationToken);

        if (resultado.IsUnauthorized)
        {
            HttpContext.Session.Clear();
            TempData["ErrorMessage"] = "Tu sesión expiró. Inicia sesión nuevamente.";
            return RedirectToAction("Login", "Account");
        }

        return View(new HomeIndexViewModel { Categorias = resultado.Data ?? [] });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
