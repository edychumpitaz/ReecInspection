using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Reec.Inspection.Api.Controllers
{
    [ApiController]
    public class DemoController : ControllerBase
    {

        private readonly ILogger<DemoController> _logger;

        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retorna el parámetro de query recibido.
        /// </summary>
        /// <param name="param1"></param>
        /// <returns></returns>
        [HttpGet(nameof(QueryParameters))]
        public IActionResult QueryParameters([FromQuery][Required] string param1)
        {
            _logger.LogInformation("Registra de manera automática en LogAudit lo que ingreso y salido del servidor.");
            return Ok(param1);
        }

        /// <summary>
        /// Retorna el body recibido.
        /// </summary>
        /// <param name="param1"></param>
        /// <returns></returns>
        [HttpPost(nameof(BodyParameters))]
        public IActionResult BodyParameters([FromBody][Required] string param1)
        {
            _logger.LogInformation("Registra de manera automática en LogAudit lo que ingreso y salido del servidor.");
            return Ok(param1);
        }


        /// <summary>
        /// Simulación de error interno del servidor 500.
        /// </summary>
        /// <returns></returns>
        [HttpGet(nameof(InternalServerError))]
        public IActionResult InternalServerError()
        {
            _logger.LogInformation("Registra de manera automática en LogAudit lo que ingreso y salido del servidor.");
            var numerador = 1;
            var denominador = 0;

            _logger.LogInformation("Registra de manera automática en LogHttp con todo el caso y pila de llamadas para replicar el error.");
            var dividendo = numerador / denominador; //forzamos el error
            return Ok(dividendo);
        }

    }
}
