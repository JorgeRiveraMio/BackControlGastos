using ControlGastos.Core.DTOs;
using ControlGastos.Core.Exceptions;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gastos")]
public sealed class GastosController(
    IGastoRepository gastoRepository,
    IGastoComprobanteService gastoComprobanteService,
    IUsuarioActualService usuarioActualService) : ControllerBase
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
            var idUsuario = usuarioActualService.ObtenerIdUsuario();
            var idGasto = await gastoRepository.RegistrarAsync(idUsuario, dto, cancellationToken);
            var respuesta = new ApiResponse<Gasto_Registrado_DTO>
            {
                IsOk = true,
                Message = "Gasto registrado correctamente.",
                Data = new Gasto_Registrado_DTO(idGasto)
            };

            return CreatedAtRoute(
                "ObtenerGastoPorId",
                new { id = idGasto },
                respuesta);
        }
        catch (GastoValidationException exception)
        {
            return BadRequest(Error(exception.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Gasto_Listar_DTO>>>> ObtenerPorMesAsync(
        int anio,
        int mes,
        CancellationToken cancellationToken)
    {
        var error = ValidarConsultaMensual(anio, mes);
        if (error is not null)
        {
            return BadRequest(Error(error));
        }

        var idUsuario = usuarioActualService.ObtenerIdUsuario();
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
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(Error("El id del gasto es obligatorio."));
        }

        var idUsuario = usuarioActualService.ObtenerIdUsuario();
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
        Gasto_Actualizar_DTO dto,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(Error("El id del gasto es obligatorio."));
        }

        var error = ValidarActualizacion(dto);
        if (error is not null)
        {
            return BadRequest(Error(error));
        }

        try
        {
            var idUsuario = usuarioActualService.ObtenerIdUsuario();
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
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(Error("El id del gasto es obligatorio."));
        }

        var idUsuario = usuarioActualService.ObtenerIdUsuario();
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

    [HttpPost("{idGasto:long}/comprobante")]
    public async Task<ActionResult<ApiResponse<ComprobanteCargado_DTO>>> SubirComprobanteAsync(
        long idGasto,
        [FromForm] IFormFile? archivo,
        CancellationToken cancellationToken)
    {
        if (idGasto <= 0)
        {
            return BadRequest(Error("El id del gasto es obligatorio."));
        }

        var idUsuario = usuarioActualService.ObtenerIdUsuario();
        var gasto = await gastoRepository.ObtenerComprobanteAsync(idGasto, idUsuario, cancellationToken);
        if (gasto is null)
        {
            return NotFound(Error("No se encontró el gasto solicitado."));
        }

        var error = ValidarArchivo(archivo);
        if (error is not null)
        {
            return BadRequest(Error(error));
        }

        try
        {
            await using var contenido = archivo!.OpenReadStream();
            var comprobante = await gastoComprobanteService.SubirAsync(
                idUsuario,
                idGasto,
                contenido,
                archivo.FileName,
                archivo.ContentType,
                cancellationToken);

            if (comprobante is null)
            {
                return NotFound(Error("No se encontró el gasto solicitado."));
            }

            return Ok(new ApiResponse<ComprobanteCargado_DTO>
            {
                IsOk = true,
                Message = "Comprobante cargado correctamente.",
                Data = new ComprobanteCargado_DTO(comprobante.NombreArchivo)
            });
        }
        catch (StorageServiceException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, Error(exception.Message));
        }
    }

    [HttpGet("{idGasto:long}/comprobante")]
    public async Task<ActionResult<ApiResponse<ComprobanteUrl_DTO>>> ObtenerComprobanteAsync(
        long idGasto,
        CancellationToken cancellationToken)
    {
        if (idGasto <= 0)
        {
            return BadRequest(Error("El id del gasto es obligatorio."));
        }

        try
        {
            var idUsuario = usuarioActualService.ObtenerIdUsuario();
            var comprobante = await gastoComprobanteService.ObtenerUrlAsync(idUsuario, idGasto, cancellationToken);
            if (comprobante is null)
            {
                return NotFound(Error("No se encontró un comprobante para el gasto solicitado."));
            }

            return Ok(new ApiResponse<ComprobanteUrl_DTO>
            {
                IsOk = true,
                Message = "Comprobante obtenido correctamente.",
                Data = comprobante
            });
        }
        catch (StorageServiceException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, Error(exception.Message));
        }
    }

    [HttpDelete("{idGasto:long}/comprobante")]
    public async Task<ActionResult<ApiResponse<object>>> EliminarComprobanteAsync(
        long idGasto,
        CancellationToken cancellationToken)
    {
        if (idGasto <= 0)
        {
            return BadRequest(Error("El id del gasto es obligatorio."));
        }

        try
        {
            var idUsuario = usuarioActualService.ObtenerIdUsuario();
            var eliminado = await gastoComprobanteService.EliminarAsync(idUsuario, idGasto, cancellationToken);
            if (!eliminado)
            {
                return NotFound(Error("No se encontró un comprobante para el gasto solicitado."));
            }

            return Ok(new ApiResponse<object>
            {
                IsOk = true,
                Message = "Comprobante eliminado correctamente."
            });
        }
        catch (StorageServiceException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, Error(exception.Message));
        }
    }

    private static string? ValidarRegistro(Gasto_Registrar_DTO dto)
    {
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

    private static string? ValidarConsultaMensual(int anio, int mes)
    {
        if (anio is < 2000 or > 2100)
        {
            return "El año debe estar entre 2000 y 2100.";
        }

        return mes is < 1 or > 12 ? "El mes debe estar entre 1 y 12." : null;
    }

    private static string? ValidarArchivo(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return "Debe adjuntar un archivo no vacío.";
        }

        if (archivo.Length > 5 * 1024 * 1024)
        {
            return "El archivo supera el tamaño máximo permitido de 5 MB.";
        }

        var contentTypeValido = archivo.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            || archivo.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            || archivo.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase);
        var extension = Path.GetExtension(Path.GetFileName(archivo.FileName));
        var extensionValida = extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);

        return contentTypeValido && extensionValida ? null : "El tipo de archivo no está permitido.";
    }

    private static ApiResponse<object> Error(string message) => new()
    {
        IsOk = false,
        Message = message
    };
}
