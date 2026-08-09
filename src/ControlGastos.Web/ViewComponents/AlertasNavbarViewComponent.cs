using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Web.ViewComponents;

public sealed class AlertasNavbarViewComponent(IControlGastosApiClient apiClient) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var token = HttpContext.Session.GetString(SessionKeys.AccessToken);
        if (string.IsNullOrWhiteSpace(token)) return View(0);
        var resultado = await apiClient.ObtenerCantidadAlertasNoLeidasAsync(token, HttpContext.RequestAborted);
        return View(resultado.IsSuccess ? resultado.Data : 0);
    }
}
