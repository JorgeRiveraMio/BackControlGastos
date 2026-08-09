using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/medios-pago")]
public sealed class MediosPagoController(IMedioPagoRepository medioPagoRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MedioPago_Listar_DTO>>>> GetAsync(
        CancellationToken cancellationToken)
    {
        var mediosPago = await medioPagoRepository.ObtenerActivosAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MedioPago_Listar_DTO>>
        {
            IsOk = true,
            Message = "Medios de pago obtenidos correctamente.",
            Data = mediosPago
        });
    }
}
