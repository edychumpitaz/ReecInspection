# 🚀 Reec.Inspection — Observabilidad ligera y trazabilidad inteligente para aplicaciones .NET

**Reec.Inspection** es una librería de observabilidad ligera para aplicaciones **.NET 8+** que centraliza:

- Auditoría de solicitudes HTTP entrantes.
- Captura automática de errores del pipeline.
- Ejecución segura de tareas en segundo plano.
- Registro de llamadas a servicios externos con resiliencia.
- Limpieza automática de logs mediante workers en segundo plano.

Todo esto usando **Entity Framework Core** y una configuración sencilla basada en opciones (`ReecExceptionOptions`).

---

## ⚡ Guía rápida

> Si solo quieres verlo funcionando en minutos, sigue esta sección.  
> Para más detalles, baja a la 👉 [Guía completa](#🧭-guía-completa).

### 📦 Instalación (NuGet)

```bash
dotnet add package Reec.Inspection
dotnet add package Reec.Inspection.SqlServer
```

---

### 🧰 Configuración mínima (`Program.cs`)

```csharp
builder.Services.AddReecInspection<InspectionDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("default")),
    options =>
    {
        options.ApplicationName = "Reec.Inspection.Api";          // Obligatorio
        options.SystemTimeZoneId = "SA Pacific Standard Time";    // Recomendado
        options.EnableProblemDetails = true;                      // Opcional
    });

var app = builder.Build();

app.UseReecInspection(); // Registra los middlewares de auditoría y captura de errores

app.MapControllers();
app.Run();
```

Con esto obtienes:

- Middleware de auditoría (`LogAudit`) para requests HTTP.
- Middleware de errores (`LogHttp`) para excepciones no controladas.
- Workers de limpieza de logs, si están habilitados en las opciones.

---

### ⚙️ Ejemplo rápido de captura de errores

```csharp
[HttpGet("error")]
public IActionResult GetError()
{
    var x = 1 / 0; // Error intencional
    return Ok();
}
```

Ese error se registra automáticamente en la tabla `LogHttp` (y puede devolverse como `ProblemDetails` si está activado).

---

### 🕒 Limpieza automática de logs

Ejemplo rápido para `LogAudit`:

```csharp
options.LogAudit.EnableClean = true;
options.LogAudit.CronValue = "0 2 * * *"; // Todos los días a las 2 a.m.
options.LogAudit.DeleteDays = 10;         // Mantiene solo los últimos 10 días
```

Cada tipo (`LogAudit`, `LogHttp`, `LogEndpoint`, `LogJob`) tiene su propio worker de limpieza opcional.

---

## 🧭 Guía completa

### Índice

1. [Configuración inicial](#1-configuración-inicial)
2. [Versión legacy vs nueva](#2-versión-legacy-vs-nueva)
3. [Configuración de `ReecExceptionOptions`](#3-configuración-de-reecexceptionoptions)
4. [Importancia de `SystemTimeZoneId`](#4-importancia-de-systemtimezoneid)
5. [Registro de `ApplicationName`](#5-registro-de-applicationname)
6. [Guardado condicional (`EnableGlobalDbSave` / `IsSaveDB`)](#6-guardado-condicional)
7. [Captura de errores (`LogHttp`, `LogAudit`)](#7-captura-de-errores-loghttp-logaudit)
8. [Ejecución de tareas en segundo plano (`IWorker`)](#8-ejecución-de-tareas-en-segundo-plano-iworker)
9. [Resiliencia en peticiones HTTP (`AddReecInspectionResilience`)](#9-resiliencia-en-peticiones-http-addreecinspectionresilience)
10. [Migración con otro proveedor de base de datos](#10-migración-con-otro-proveedor-de-base-de-datos)
11. [Buenas prácticas y sugerencias](#11-buenas-prácticas-y-sugerencias)
12. [Estado del proyecto](#12-estado-del-proyecto)

---

## 1. Configuración inicial

Registro principal en `Program.cs`:

```csharp
builder.Services.AddReecInspection<InspectionDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("default")),
    options =>
    {
        options.ApplicationName = "Reec.Inspection.Api";
        options.SystemTimeZoneId = "SA Pacific Standard Time";
        options.EnableProblemDetails = true;
        options.EnableGlobalDbSave = true;
    });

var app = builder.Build();

app.UseReecInspection();
```

`AddReecInspection`:

- Registra el `DbContext` derivado de `InspectionDbContext` con `DbContextPool`.
- Registra middlewares (`LogAuditMiddleware`, `LogHttpMiddleware`).
- Registra `IWorker`, `IDateTimeService` y workers de limpieza (`CleanLog*Worker`) según configuración.
- Opcionalmente agrega soporte `ProblemDetails`.

`UseReecInspection`:

- Agrega al pipeline los middlewares de auditoría y captura de errores según `ReecExceptionOptions`.

Orden recomendado:

```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseReecInspection();
app.MapControllers();
```

---

## 2. Versión legacy vs nueva

### `AddReecException<TDbContext>` (Legacy)

- Se mantiene con `[Obsolete]` para compatibilidad.
- Usar solo en proyectos existentes.

### `AddReecInspection<TDbContext>` (Recomendado)

- Nueva API principal.
- Usa `DbContextPool`.
- Configura `ReecExceptionOptions` mediante `Action<ReecExceptionOptions>`.
- Integra hosted services de limpieza y `IWorker`.

Ejemplo:

```csharp
builder.Services.AddReecInspection<InspectionDbContext>(
    options => options.UseSqlServer(connString),
    options =>
    {
        options.ApplicationName = "MyService.Api";
        options.EnableGlobalDbSave = true;
        options.LogHttp.IsSaveDB = true;
        options.LogAudit.IsSaveDB = true;
    });
```

---

## 3. Configuración de `ReecExceptionOptions`

`ReecExceptionOptions` centraliza la configuración global.

### Propiedades principales

| Propiedad | Descripción | Default |
|----------|-------------|--------|
| `ApplicationName` | Nombre de la app que genera los logs. | `null` |
| `ApplicationErrorMessage` | Mensaje mostrado cuando ocurre un error al intentar guardar información en la base de datos. | `Ocurrió un error al guardar log en Base de Datos. ` |
| `InternalServerErrorMessage` | Mensaje genérico utilizado para errores internos del sistema. | `Error no controlado del sistema.` |
| `SystemTimeZoneId` | Zona horaria usada para registrar fechas y programar cron. | `"SA Pacific Standard Time"` |
| `EnableMigrations` | Ejecuta migraciones automáticas al inicio. | `true` |
| `EnableProblemDetails` | Respuestas de error en formato `ProblemDetails`. | `false` |
| `EnableGlobalDbSave` | Habilita/deshabilita escritura global en BD. | `true` |
| `MinCategory` | Categoría mínima a registrar. | `Unauthorized` (401) |

### Secciones por tipo de log

Cada módulo tiene opciones propias (`LogAudit`, `LogHttp`, `LogJob`, `LogEndpoint`):

- `Schema`: esquema de base de datos.
- `TableName`: nombre de la tabla.
- `IsSaveDB`: habilita/deshabilita persistencia.
- `EnableClean`: activa worker de limpieza.
- `CronValue`: expresión CRON para limpieza.
- `DeleteDays`: días hacia atrás a conservar.
- `DeleteBatch`: tamaño del lote de borrado.

Ejemplo para `LogAudit` con tablas existentes (sin migraciones):

```csharp
options.EnableMigrations = false;

options.LogAudit.Schema = "Inspection";
options.LogAudit.TableName = "LogAudit";
options.LogAudit.IsSaveDB = true;
options.LogAudit.EnableClean = true;
options.LogAudit.CronValue = "0 2 * * *";
options.LogAudit.DeleteDays = 15;
options.LogAudit.DeleteBatch = 500;
```

Aplica el mismo patrón para `LogHttp`, `LogJob` y `LogEndpoint`.

---

## 4. Importancia de `SystemTimeZoneId`

Todas las fechas registradas en los logs y workers usan esta zona horaria:

- Fechas de creación.
- Ejecuciones de jobs.
- Cálculo de `CronValue`.

```csharp
options.SystemTimeZoneId = "SA Pacific Standard Time";
```

Para ver las zonas disponibles:

```csharp
var zones = TimeZoneInfo.GetSystemTimeZones();
```

Si el ID es inválido, la inicialización de `IDateTimeService` lanzará excepción.

---

## 5. Registro de `ApplicationName`

Obligatorio para distinguir qué sistema originó cada registro.

```csharp
options.ApplicationName = "Billing.Api";
```

Se utiliza en todas las tablas de log como columna de referencia.

---

## 6. Guardado condicional

### Global

```csharp
options.EnableGlobalDbSave = true; // Si es false, no se persisten logs en BD.
```

### Por módulo

```csharp
options.LogAudit.IsSaveDB = true;
options.LogHttp.IsSaveDB = true;
options.LogJob.IsSaveDB = true;
options.LogEndpoint.IsSaveDB = true;
```

Desactivar por módulo es útil para escenarios donde solo quieres ciertos tipos de trazas.

---

## 7. Captura de errores (`LogHttp`, `LogAudit`)

### `LogHttp` — Errores del pipeline

Ejemplo:

```csharp
[HttpGet("test-error")]
public IActionResult TestError()
{
    var value = 10 / 0;
    return Ok(value);
}
```

`LogHttpMiddleware`:

- Intercepta excepciones no controladas.
- Registra `Exception`, `StackTrace`, `Path`, `TraceIdentifier`, etc.
- Puede responder en `ProblemDetails` si está habilitado.

### `LogAudit` — Auditoría HTTP

`LogAuditMiddleware`:

- Registra método, ruta, estado, tiempos, opcionalmente cuerpo de request/response.
- Respeta:
  - `ExcludePaths`
  - `RequestBodyMaxSize`
  - `ResponseBodyMaxSize`
  - `EnableBuffering`

Ejemplo de exclusión:

```csharp
options.LogAudit.ExcludePaths = new[] { "swagger", "health", "index" };
```

---

## 8. Ejecución de tareas en segundo plano (`IWorker`)

`IWorker` expone:

- `RunFunction`: lógica principal.
- `RunFunctionException`: manejo custom de errores.
- `IsLightExecution`: solo registra fallos cuando es `true`.
- Estados (`Enqueued`, `Processing`, `Succeeded`, `Failed`) en `LogJob`.

### 8.1 Modo persistente (HostedService)

Uso recomendado para jobs periódicos (patrón similar a los `CleanLog*Worker`).

```csharp
public class SampleJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SampleJobWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            worker.NameJob = nameof(SampleJobWorker);
            worker.CreateUser = "System";
            worker.IsLightExecution = false;

            worker.RunFunction = service => ProcessAsync(service, stoppingToken);

            await worker.ExecuteAsync(stoppingToken);
        }
    }

    private static async Task<string> ProcessAsync(IServiceProvider services, CancellationToken ct)
    {
        var dbContextService = services.GetRequiredService<IDbContextService>();
        var db = dbContextService.GetDbContext();

        // Lógica de negocio aquí
        await Task.Delay(1000, ct);

        return "Proceso completado correctamente.";
    }
}
```

Registrar el worker:

```csharp
builder.Services.AddHostedService<SampleJobWorker>();
```

---

### 8.2 Modo “fire-and-forget” disparado desde un request

Para iniciar una tarea en segundo plano desde un endpoint HTTP sin bloquear la respuesta:

```csharp
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public JobsController(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [HttpPost("start-clean-temp")]
    public IActionResult StartCleanTemp()
    {
        var scope = _scopeFactory.CreateScope();
        var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

        worker.NameJob = "CleanTemporaryFiles";
        worker.CreateUser = "System";
        worker.IsLightExecution = true;

        worker.RunFunction = svc => ProcessAsync(svc);

        _ = worker.ExecuteAsync().ContinueWith(_ =>
        {
            scope.Dispose();
        });

        return Ok("Tarea en segundo plano iniciada.");
    }

    private static async Task<string> ProcessAsync(IServiceProvider services)
    {
        var dbContextService = services.GetRequiredService<IDbContextService>();
        var db = dbContextService.GetDbContext();

        // Lógica puntual
        await Task.Delay(2000);
        return "Limpieza de temporales completada.";
    }
}
```

Notas:

- No se usa `Task.Run` externo: `IWorker` maneja la ejecución asíncrona y logging.
- No se espera el resultado, pero el job queda registrado en `LogJob`.

---

## 9. Resiliencia en peticiones HTTP (`AddReecInspectionResilience`)

Esta extensión integra:

- `LogEndpointHandler`: registra requests/responses a servicios externos.
- Pipeline estándar de resiliencia (timeout, retry, circuit breaker).

### Registro

```csharp
var httpBuilder = builder.Services.AddHttpClient("PlaceHolder", httpClient =>
{
    httpClient.DefaultRequestHeaders.Clear();
    httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
});

builder.Services.AddReecInspectionResilience(httpBuilder);
```

Uso:

```csharp
public class ExternalController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts()
    {
        var client = _httpClientFactory.CreateClient("PlaceHolder");
        var response = await client.GetAsync("/posts");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}
```

`AddReecInspectionResilience` configura por defecto:

- Timeout total: 1 minuto (personalizable).
- Reintentos con backoff exponencial.
- Circuit breaker con metadatos en `HttpRequestMessage.Options`.

---

## 10. Migración con otro proveedor de base de datos

Para usar PostgreSQL (u otro proveedor soportado por EF Core), hereda de `InspectionDbContext` y genera una migración:

```csharp
public class InspectionPgContext : InspectionDbContext
{
    public InspectionPgContext(DbContextOptions<InspectionPgContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ejemplo: esquema por defecto
        modelBuilder.HasDefaultSchema("inspection");
    }
}
```

Registro:

```csharp
builder.Services.AddReecInspection<InspectionPgContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")),
    options =>
    {
        options.ApplicationName = "Reec.Pg.Api";
        options.EnableMigrations = true; // O manejar migraciones externamente
    });
```

---

## 11. Buenas prácticas y sugerencias

- Configura siempre `ApplicationName` y `SystemTimeZoneId`.
- Considera desactivar `EnableMigrations` en producción y aplicar migraciones vía CI/CD.
- Usa `Schema` dedicado (ej. `"Inspection"`) para aislar tus tablas de log.
- Ajusta `DeleteDays` y `DeleteBatch` según volumen de logs.
- Usa `IsLightExecution = true` para jobs muy frecuentes donde solo te interesen errores.
- Excluye rutas sensibles de `LogAudit` (`swagger`, `health`, etc.).
- Asegúrate de no registrar cuerpos que contengan datos sensibles sin anonimizar.

---

## 12. Estado del proyecto

- Repositorio: [github.com/edychumpitaz/ReecInspection](https://github.com/edychumpitaz/ReecInspection)
- Autor: **Edy Chumpitaz**
- Proximamente documentación completa estilo ReadTheDocs.

> Si tienes sugerencias, PRs o issues, son bienvenidos para seguir puliendo la librería 💻
