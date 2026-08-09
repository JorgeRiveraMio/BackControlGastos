using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alertas")]
public sealed class AlertasController(IAlertaRepository alertaRepository, IUsuarioActualService usuarioActualService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Alerta_Listar_DTO>>>> ListarAsync(CancellationToken cancellationToken) =>
        Ok(new ApiResponse<IReadOnlyList<Alerta_Listar_DTO>> { IsOk = true, Message = "Alertas obtenidas correctamente.", Data = await alertaRepository.ListarAsync(usuarioActualService.ObtenerIdUsuario(), cancellationToken) });

    [HttpGet("no-leidas/count")]
    public async Task<ActionResult<ApiResponse<int>>> ContarNoLeidasAsync(CancellationToken cancellationToken) =>
        Ok(new ApiResponse<int> { IsOk = true, Message = "Cantidad de alertas no leídas obtenida correctamente.", Data = await alertaRepository.ContarNoLeidasAsync(usuarioActualService.ObtenerIdUsuario(), cancellationToken) });

    [HttpPut("{id:long}/leida")]
    public async Task<ActionResult<ApiResponse<object>>> MarcarLeidaAsync(long id, CancellationToken cancellationToken)
    {
        await alertaRepository.MarcarLeidaAsync(usuarioActualService.ObtenerIdUsuario(), id, cancellationToken);
        return Ok(new ApiResponse<object> { IsOk = true, Message = "Alerta marcada como leída." });
    }
}
