using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Models.ViewModels;
using ControlGastos.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Web.Controllers;

[SessionAuthorize]
public sealed class DashboardController(IControlGastosApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? anio, int? mes, CancellationToken cancellationToken)
    {
        var ahora = FechaLimaHelper.ObtenerAhoraLima();
        var año = anio is >= 2000 and <= 2100 ? anio.Value : ahora.Year;
        var mesSeleccionado = mes is >= 1 and <= 12 ? mes.Value : ahora.Month;
        var resultado = await apiClient.ObtenerDashboardAsync(Token(), año, mesSeleccionado, cancellationToken);
        if (SesionExpirada(resultado)) return Login();
        if (!resultado.IsSuccess || resultado.Data is null)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "No fue posible obtener el resumen mensual.";
            return View(new DashboardViewModel { Anio = año, Mes = mesSeleccionado });
        }
        var resumen = resultado.Data;
        return View(new DashboardViewModel
        {
            Anio = año, Mes = mesSeleccionado, IngresoMensual = resumen.IngresoMensual,
            PresupuestoMensual = resumen.PresupuestoMensual, TotalGastado = resumen.TotalGastado,
            DisponibleEstimado = resumen.DisponibleEstimado, PorcentajePresupuestoConsumido = resumen.PorcentajePresupuestoConsumido,
            PorcentajeAlerta = resumen.PorcentajeAlerta, CantidadGastos = resumen.CantidadGastos,
            PromedioGasto = resumen.PromedioGasto, CategoriaMayorGasto = resumen.CategoriaMayorGasto,
            MontoCategoriaMayorGasto = resumen.MontoCategoriaMayorGasto,
            GastosPorCategoria = resumen.GastosPorCategoria, UltimosGastos = resumen.UltimosGastos
        });
    }
    private string Token() => HttpContext.Session.GetString(SessionKeys.AccessToken)!;
    private bool SesionExpirada<T>(ApiClientResult<T> resultado) { if (!resultado.IsUnauthorized) return false; HttpContext.Session.Clear(); return true; }
    private IActionResult Login() => RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index)) });
}
