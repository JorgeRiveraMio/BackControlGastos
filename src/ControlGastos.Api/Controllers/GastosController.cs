using ControlGastos.Core.DTOs;
using ControlGastos.Core.Exceptions;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Route("api/gastos")]
public sealed class GastosController(IGastoRepository gastoRepository) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Gasto_Registrado_DTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Gasto_Registrado_DTO>>> RegistrarAsync(
        Gasto_Registrar_DTO dto,
        CancellationToken cancellationToken)
    {
        var error = ValidarRegistro(dto);
        if (error is not null)
        {
            return BadRequest(Error(error));
        }

        try
        {
            var idGasto = await gastoRepository.RegistrarAsync(dto, cancellationToken);
            var respuesta = new ApiResponse<Gasto_Registrado_DTO>
            {
                IsOk = true,
                Message = "Gasto registrado correctamente.",
                Data = new Gasto_Registrado_DTO(idGasto)
            };

            return CreatedAtRoute(
                "ObtenerGastoPorId",
                new { id = idGasto, idUsuario = dto.IdUsuario },
                respuesta);
        }
        catch (GastoValidationException exception)
        {
            return BadRequest(Error(exception.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Gasto_Listar_DTO>>>> ObtenerPorMesAsync(
        Guid idUsuario,
        int anio,
        int mes,
        CancellationToken cancellationToken)
    {
        var error = ValidarConsultaMensual(idUsuario, anio, mes);
        if (error is not null)
        {
            return BadRequest(Error(error));
        }

        var gastos = await gastoRepository.ObtenerPorMesAsync(idUsuario, anio, mes, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<Gasto_Listar_DTO>>
        {
            IsOk = true,
            Message = "Gastos obtenidos correctamente.",
            Data = gastos
        });
    }

    [HttpGet("{id:long}", Name = "ObtenerGastoPorId")]
    public async Task<ActionResult<ApiResponse<Gasto_Listar_DTO>>> ObtenerPorIdAsync(
        long id,
        Guid idUsuario,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || idUsuario == Guid.Empty)
        {
            return BadRequest(Error("El id del gasto y el id del usuario son obligatorios."));
        }

        var gasto = await gastoRepository.ObtenerPorIdAsync(id, idUsuario, cancellationToken);
        if (gasto is null)
        {
            return NotFound(Error("No se encontró el gasto solicitado."));
        }

        return Ok(new ApiResponse<Gasto_Listar_DTO>
        {
            IsOk = true,
            Message = "Gasto obtenido correctamente.",
            Data = gasto
        });
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> ActualizarAsync(
        long id,
        Guid idUsuario,
        Gasto_Actualizar_DTO dto,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || idUsuario == Guid.Empty)
        {
            return BadRequest(Error("El id del gasto y el id del usuario son obligatorios."));
        }

        var error = ValidarActualizacion(dto);
        if (error is not null)
        {
            return BadRequest(Error(error));
        }

        try
        {
            var actualizado = await gastoRepository.ActualizarAsync(id, idUsuario, dto, cancellationToken);
            if (!actualizado)
            {
                return NotFound(Error("No se encontró el gasto solicitado."));
            }

            return Ok(new ApiResponse<object>
            {
                IsOk = true,
                Message = "Gasto actualizado correctamente."
            });
        }
        catch (GastoValidationException exception)
        {
            return BadRequest(Error(exception.Message));
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> AnularAsync(
        long id,
        Guid idUsuario,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || idUsuario == Guid.Empty)
        {
            return BadRequest(Error("El id del gasto y el id del usuario son obligatorios."));
        }

        var anulado = await gastoRepository.AnularAsync(id, idUsuario, cancellationToken);
        if (!anulado)
        {
            return NotFound(Error("No se encontró el gasto solicitado."));
        }

        return Ok(new ApiResponse<object>
        {
            IsOk = true,
            Message = "Gasto anulado correctamente."
        });
    }

    private static string? ValidarRegistro(Gasto_Registrar_DTO dto)
    {
        if (dto.IdUsuario == Guid.Empty)
        {
            return "El id del usuario es obligatorio.";
        }

        return ValidarDatosGasto(dto.IdCategoriaGasto, dto.Monto, dto.FechaGasto, dto.NombreComercio, dto.Descripcion);
    }

    private static string? ValidarActualizacion(Gasto_Actualizar_DTO dto) =>
        ValidarDatosGasto(dto.IdCategoriaGasto, dto.Monto, dto.FechaGasto, dto.NombreComercio, dto.Descripcion);

    private static string? ValidarDatosGasto(
        int idCategoriaGasto,
        decimal monto,
        DateTimeOffset fechaGasto,
        string? nombreComercio,
        string? descripcion)
    {
        if (idCategoriaGasto <= 0)
        {
            return "La categoría de gasto es obligatoria.";
        }

        if (monto <= 0)
        {
            return "El monto debe ser mayor que cero.";
        }

        if (fechaGasto == default)
        {
            return "La fecha del gasto es obligatoria.";
        }

        if (nombreComercio?.Length > 150)
        {
            return "El nombre del comercio no puede superar 150 caracteres.";
        }

        return descripcion?.Length > 300
            ? "La descripción no puede superar 300 caracteres."
            : null;
    }

    private static string? ValidarConsultaMensual(Guid idUsuario, int anio, int mes)
    {
        if (idUsuario == Guid.Empty)
        {
            return "El id del usuario es obligatorio.";
        }

        if (anio is < 2000 or > 2100)
        {
            return "El año debe estar entre 2000 y 2100.";
        }

        return mes is < 1 or > 12 ? "El mes debe estar entre 1 y 12." : null;
    }

    private static ApiResponse<object> Error(string message) => new()
    {
        IsOk = false,
        Message = message
    };
}
