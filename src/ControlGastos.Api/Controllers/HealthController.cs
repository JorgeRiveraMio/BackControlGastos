using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ControlGastos.Infrastructure.Data;

namespace ControlGastos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ControlGastosDbContext _dbContext;

        public HealthController(ControlGastosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                isOk = true,
                message = "ControlGastos API funcionando"
            });
        }
        [HttpGet("database")]
        public async Task<IActionResult> GetDatabaseStatusAsync(
    CancellationToken cancellationToken)
        {
            try
            {
                bool canConnect = await _dbContext.Database
                    .CanConnectAsync(cancellationToken);

                if (!canConnect)
                {
                    return StatusCode(
                        StatusCodes.Status503ServiceUnavailable,
                        new
                        {
                            isOk = false,
                            message = "No se pudo conectar con PostgreSQL."
                        });
                }

                return Ok(new
                {
                    isOk = true,
                    message = "Conexión con Supabase PostgreSQL correcta."
                });
            }
            catch (Exception exception)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        isOk = false,
                        message = "Error al conectar con PostgreSQL.",
                        detail = exception.Message
                    });
            }
        }
    }
}