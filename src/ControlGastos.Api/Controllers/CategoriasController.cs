using ControlGastos.Core.DTOs;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public sealed class CategoriasController(ICategoriaGastoRepository categoriaGastoRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoriaGasto_Listar_DTO>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoriaGasto_Listar_DTO>>>> GetAsync(
        CancellationToken cancellationToken)
    {
        var categorias = await categoriaGastoRepository.ObtenerActivasAsync(cancellationToken);

        return Ok(new ApiResponse<IReadOnlyList<CategoriaGasto_Listar_DTO>>
        {
            IsOk = true,
            Message = "Categorías obtenidas correctamente.",
            Data = categorias
        });
    }
}
