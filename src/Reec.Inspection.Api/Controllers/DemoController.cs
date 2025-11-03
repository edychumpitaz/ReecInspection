using Microsoft.AspNetCore.Mvc;

namespace Reec.Inspection.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : ControllerBase
    {

        private readonly ILogger<DemoController> _logger;

        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
        }


        [HttpGet(nameof(GetQueryParameters))]
        public IActionResult GetQueryParameters([FromQuery] string param1)
        {
            _logger.LogInformation("Registra de manera automática en LogAudit lo que ingreso y salido del servidor.");
            return Ok(param1);
        }

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
