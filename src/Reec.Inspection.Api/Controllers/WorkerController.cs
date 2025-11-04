using Microsoft.AspNetCore.Mvc;
using Reec.Inspection.Workers;

namespace Reec.Inspection.Api.Controllers
{
    [ApiController]
    public class WorkerController : Controller
    {
        private readonly IServiceScopeFactory _serviceScope;
        public WorkerController(IServiceScopeFactory serviceScope)
        {
            this._serviceScope = serviceScope;
        }


        [HttpGet(nameof(RunWorker))]
        public IActionResult RunWorker()
        {
            var scope = _serviceScope.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = nameof(RunWorkerError);
            worker.Delay = TimeSpan.FromSeconds(2);
            worker.RunFunction = (service) =>
            {
                return Task.FromResult("Se ejecutó correctamente.");
            };
            _ = worker.ExecuteAsync();
            return Ok("Se envió la ejecución en segundo plano");
        }



        [HttpGet(nameof(RunWorkerError))]
        public IActionResult RunWorkerError()
        {
            var scope = _serviceScope.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = nameof(RunWorkerError);
            worker.Delay = TimeSpan.FromSeconds(2);
            worker.RunFunction = (service) =>
            {
                var numerador = 1;
                var denominador = 0;
                var dividendo = numerador / denominador;
                return Task.FromResult("Se ejecutó correctamente.");
            };
            _ = worker.ExecuteAsync();
            return Ok("Se envió la ejecución en segundo plano");
        }

        [HttpGet(nameof(RunWorkerCatchError))]
        public IActionResult RunWorkerCatchError()
        {
            var scope = _serviceScope.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = nameof(RunWorkerCatchError);
            worker.Delay = TimeSpan.FromSeconds(2);
            worker.RunFunction = (service) =>
            {
                var numerador = 1;
                var denominador = 0;
                var dividendo = numerador / denominador;
                return Task.FromResult("Se ejecutó correctamente.");
            };
            worker.RunFunctionException = (service, exception) =>
            {
                //Se captura error para procesarlo.
                var logger = service.GetRequiredService<ILogger<DemoController>>();
                logger.LogError(exception, "Se capturó el error de forma segura.");
                return Task.CompletedTask;
            };

            _ = worker.ExecuteAsync();
            return Ok("Se envió la ejecución en segundo plano");
        }


        [HttpGet(nameof(RunWorkerCatchErrorOfError))]
        public IActionResult RunWorkerCatchErrorOfError()
        {
            var scope = _serviceScope.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = nameof(RunWorkerCatchErrorOfError);
            worker.Delay = TimeSpan.FromSeconds(2);
            worker.RunFunction = (service) =>
            {
                var numerador = 1;
                var denominador = 0;
                var dividendo = numerador / denominador;
                return Task.FromResult("Se ejecutó correctamente.");
            };
            worker.RunFunctionException = (service, exception) =>
            {
                //Se captura error para procesarlo.
                var logger = service.GetRequiredService<ILogger<DemoController>>();
                logger.LogError(exception, "Se capturó el error de forma segura.");

                //Simulación de proceso que genera otro error.
                //En la tabla LogJob se captura todo el trace del error.
                var numerador = 10;
                var denominador = 0;
                var dividendo = numerador / denominador;

                return Task.CompletedTask;
            };

            _ = worker.ExecuteAsync();
            return Ok("Se envió la ejecución en segundo plano");
        }


    }
}
