# Workers (`IWorker`)

El componente **`IWorker`** estandariza la ejecución de tareas en segundo plano con **trazabilidad**, **manejo homogéneo de errores** y **registro en `LogJob`**.
La idea es que cualquier job (periódico o bajo demanda) tenga el mismo “ciclo de vida” y el mismo formato de observabilidad.

---

## 1. ¿Qué es un Worker en Reec Inspection?

Un **Worker** encapsula:

- **Metadatos**
  - `NameJob`: Nombre legible del job.
  - `CreateUser`: Quién inició la ejecución (sistema / usuario / admin endpoint).
  - `IsLightExecution`: Si es `true`, registra **solo fallos**. Si es `false`, registra el ciclo completo.
  - `TraceIdentifier`: Identificador de trazabilidad que permite correlacionar la ejecución del worker con otros procesos
  (HTTP request, job previo, proceso externo, etc.).  
  Si no se define explícitamente, la librería puede generar uno automáticamente.
  - `Delay` : Permite retrasar la ejecución del worker antes de iniciar `RunFunction`.
  Es útil para:
    - Ejecuciones diferidas.
    - Reintentos controlados.
    - Escenarios donde se requiere esperar recursos externos.
    - Si es `null`, la ejecución inicia inmediatamente.

- **Ejecución**
  - `RunFunction`: Lógica principal del job.
  - `RunFunctionException`: Manejo custom de excepciones (opcional).
  - `ExecuteAsync(CancellationToken)`: Ejecuta el job y registra el resultado.

> Recomendación: **No uses `Task.Run()`** dentro de `RunFunction`. La ejecución ya es asíncrona y el registro del ciclo de vida lo controla `IWorker`.

> 💡 **Nota**  
> `Delay` no reemplaza un scheduler ni un CRON.  
> Su objetivo es controlar el inicio de la ejecución **dentro del ciclo de vida del Worker**,
> manteniendo trazabilidad y manejo homogéneo de errores.

---

## 2. Patrón recomendado: HostedService (jobs periódicos)

Este modo es ideal para tareas programadas (limpiezas, consolidaciones, sincronizaciones, etc.).

### 2.1 Ejemplo: job periódico con `BackgroundService`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class SampleJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SampleJobWorker(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Frecuencia del job (ejemplo)
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            worker.NameJob = nameof(SampleJobWorker);
            worker.CreateUser = "System";
            worker.IsLightExecution = false;

            worker.RunFunction = services => ProcessAsync(services, stoppingToken);

            // Ejecución controlada + registro en LogJob
            await worker.ExecuteAsync(stoppingToken);
        }
    }

    private static async Task<string> ProcessAsync(IServiceProvider services, CancellationToken ct)
    {
        // Resuelve dependencias aquí (DbContext, clients, etc.)
        // var db = services.GetRequiredService<InspectionDbContext>();

        await Task.Delay(1000, ct);
        return "Job ejecutado correctamente.";
    }
}
```

### 2.2 Registro del HostedService

```csharp
builder.Services.AddHostedService<SampleJobWorker>();
```

---

## 3. Modo bajo demanda (API / Endpoint administrativo)

Útil cuando necesitas disparar una tarea por comando y dejarla registrada en `LogJob`.

### 3.1 Ejemplo: disparar un job desde un endpoint

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

[ApiController]
[Route("jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public JobsController(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    [HttpPost("start-clean-temp")]
    public async Task<IActionResult> StartCleanTemp(CancellationToken ct)
    {
        var scope = _scopeFactory.CreateScope();
        var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

        worker.NameJob = "CleanTemporaryFiles";
        worker.CreateUser = "System";
        worker.IsLightExecution = false;

        worker.RunFunction = services => CleanTempAsync(services, ct);

        await worker.ExecuteAsync(ct)
            .ContinueWith(_ =>
            {
                scope.Dispose();
            });

        return Ok(new { message = "Job ejecutado. Revisa LogJob para el detalle." });
    }

    private static async Task<string> CleanTempAsync(IServiceProvider services, CancellationToken ct)
    {
        await Task.Delay(1500, ct);
        return "Limpieza de temporales completada.";
    }
}
```

---

## 4. Manejo custom de errores (`RunFunctionException`)

Cuando quieres controlar **qué se registra** y/o **qué mensaje queda en LogJob** ante excepciones, usa `RunFunctionException`.

### 4.1 Ejemplo: capturar la excepción y devolver un mensaje controlado

```csharp
worker.RunFunction = services => ProcessAsync(services, ct);

// Manejo custom: convertir excepción en un mensaje normalizado
worker.RunFunctionException = (services, ex) =>
{
    //Puedes enriquecer datos de error en la propiedad nativa de Exception.
    //Data es un diccionario donde puedes agregar información util del error y se grabará en base de datos.
    ex.Data.Add("Contexto 1", "error 1");
    ex.Data.Add("Contexto 2", "error 2");

    // Aquí puedes enriquecer el error con contexto propio
    // (correlation id, tenant, endpoint, etc.)
    return Task.FromResult($"Fallo controlado: {ex.GetType().Name} - {ex.Message}");
};

await worker.ExecuteAsync(ct);
```

### 4.2 Ejemplo: reintento simple (solo como idea)

```csharp
worker.RunFunctionException = async (ex, services) =>
{
    // Reintento único para errores transitorios
    await Task.Delay(500);
    return $"Error transitorio detectado. Se intentó re-ejecutar. Detalle: {ex.Message}";
};
```

> Recomendación: para reintentos reales, preferir políticas (ej. Polly) en el componente que falla (HTTP client, DB, etc.), no en el Worker.

---

## 5. Limpieza automática de logs (por módulo)

Además de workers “propios”, Reec Inspection soporta limpieza automática por módulo (según tus opciones: `EnableClean`, `CronValue`, `DeleteDays`, `DeleteBatch`).

### 5.1 Configuración (Program.cs)

Ejemplo típico para habilitar limpieza automática en **LogAudit** y **LogHttp**:

```csharp
builder.Services.AddReecInspection<DbContextSqlServer>(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    options =>
    {
        // LogAudit
        options.LogAudit.EnableClean = true;
        options.LogAudit.CronValue = "0 2 * * *"; // 2:00 a.m.
        options.LogAudit.DeleteDays = 10;
        options.LogAudit.DeleteBatch = 500;
        options.LogAudit.Schema = "Inspection";
        options.LogAudit.TableName = "LogAudit";

        // LogHttp
        options.LogHttp.EnableClean = true;
        options.LogHttp.CronValue = "0 3 * * *"; // 3:00 a.m.
        options.LogHttp.DeleteDays = 15;
        options.LogHttp.DeleteBatch = 500;
        options.LogHttp.Schema = "Inspection";
        options.LogHttp.TableName = "LogHttp";
    });
```

### 5.2 ¿Cómo se ejecuta la limpieza?

La ejecución de limpieza depende de tu implementación interna, pero el patrón esperado es:

- Un **scheduler** (interno o un HostedService) que evalúa el CRON. Puedes usar [https://crontab.guru](https://crontab.guru) para generar y validar expresiones CRON.
- Un proceso que ejecuta la eliminación por lotes (`DeleteBatch`) hasta cumplir la retención (`DeleteDays`).

> Recomendación: define horarios escalonados por módulo (2:00, 3:00, 4:00 a.m.) para evitar picos simultáneos de IO/locks.

### 5.3 Buenas prácticas de limpieza

- Usa `DeleteBatch` moderado (ej. 200–1000) para no bloquear la BD.
- Indexa por fecha (columna de creación) si esperas alta volumetría.
- Separa por `Schema` dedicado (ej. `Inspection`) para aislar el dominio de logs.

---

## 6. Buenas prácticas generales

- Usa `IsLightExecution = true` en jobs frecuentes donde solo te importan los fallos.
- Asigna `NameJob` estable (no uses GUID) para que sea agregable por métricas.
- Resuelve dependencias dentro del `scope` (DbContext, repos, clients).
- Evita lógica pesada en memoria si el job puede stream/iterar por lotes.
