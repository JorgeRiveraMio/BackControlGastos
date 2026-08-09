using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/perfil")]
public sealed class PerfilController(
    IPerfilRepository perfilRepository,
    IUsuarioActualService usuarioActualService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PerfilFinanciero_DTO>>> ObtenerAsync(CancellationToken cancellationToken)
    {
        var perfil = await perfilRepository.ObtenerAsync(usuarioActualService.ObtenerIdUsuario(), cancellationToken);
        return Ok(new ApiResponse<PerfilFinanciero_DTO>
        {
            IsOk = true,
            Message = "Perfil obtenido correctamente.",
            Data = perfil
        });
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<object>>> GuardarAsync(
        PerfilFinanciero_Actualizar_DTO dto, CancellationToken cancellationToken)
    {
        var error = Validar(dto);
        if (error is not null) return BadRequest(Error(error));

        await perfilRepository.GuardarAsync(usuarioActualService.ObtenerIdUsuario(), dto, cancellationToken);
        return Ok(new ApiResponse<object> { IsOk = true, Message = "Perfil guardado correctamente." });
    }

    private static string? Validar(PerfilFinanciero_Actualizar_DTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreUsuario)) return "El nombre es obligatorio.";
        if (dto.NombreUsuario.Length > 120) return "El nombre no puede superar 120 caracteres.";
        if (dto.IngresoMensual < 0 || dto.PresupuestoMensual < 0) return "Los montos no pueden ser negativos.";
        return dto.PorcentajeAlerta is <= 0 or > 100 ? "El porcentaje de alerta debe estar entre 0 y 100." : null;
    }

    private static ApiResponse<object> Error(string message) => new() { IsOk = false, Message = message };
}
