using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(
    IDashboardRepository dashboardRepository,
    IUsuarioActualService usuarioActualService) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<ActionResult<ApiResponse<DashboardResumenMensual_DTO>>> ObtenerResumenAsync(
        int anio, int mes, CancellationToken cancellationToken)
    {
        if (anio is < 2000 or > 2100 || mes is < 1 or > 12)
        {
            return BadRequest(new ApiResponse<object> { IsOk = false, Message = "El año o mes indicado no es válido." });
        }

        var resumen = await dashboardRepository.ObtenerResumenMensualAsync(
            usuarioActualService.ObtenerIdUsuario(), anio, mes, cancellationToken);
        return Ok(new ApiResponse<DashboardResumenMensual_DTO>
        {
            IsOk = true,
            Message = "Resumen mensual obtenido correctamente.",
            Data = resumen
        });
    }
}
