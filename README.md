# 🚀 Reec.Inspection — Observabilidad ligera y trazabilidad para aplicaciones .NET

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

### 💝 Apoya el desarrollo continuo

Si **Reec.Inspection** está ayudando a optimizar tu trabajo y te gustaría contribuir al desarrollo continuo de esta librería, puedes hacerlo a través de **Plin** (Perú):

<div align="center">

<img src="images/QR Plin.jpeg" alt="Plin QR Code" width="300"/>

**Yape/Plin** 

</div>

Tu apoyo ayuda a mantener el proyecto actualizado con nuevas características, correcciones de bugs y documentación mejorada. ¡Toda contribución es valorada! 🙏

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
12. [Manejo de excepciones controladas (Modo Legacy)](#12-manejo-de-excepciones-controladas-modo-legacy)
13. [Manejo de excepciones con ProblemDetails (Modo Actual)](#13-manejo-de-excepciones-con-problemdetails-modo-actual)
14. [Estado del proyecto](#14-estado-del-proyecto)

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
app.UseReecInspection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
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
- `CronValue`: expresión CRON para limpieza. Puedes usar [https://crontab.guru](https://crontab.guru) para generar y validar expresiones CRON.
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

### Configuración de `EnableBuffering` en `LogHttp` y `LogAudit`

La propiedad `EnableBuffering` está disponible únicamente en los módulos `LogHttp` y `LogAudit`, ya que estos middlewares necesitan leer el cuerpo (body) de las peticiones y respuestas HTTP para registrarlas en la base de datos.

**¿Qué hace `EnableBuffering`?**

Cuando está habilitado (`true`), permite que el stream del request y response pueda ser leído múltiples veces, lo cual es necesario para capturar el contenido sin afectar el flujo normal de la aplicación.

**¿Cuándo desactivarlo?**

Si ya tienes un middleware superior en tu pipeline que gestiona el buffering del request/response (por ejemplo, para logging personalizado, transformación de contenido, o compresión), puedes desactivar `EnableBuffering` en estos módulos para evitar redundancia y mejorar el rendimiento.

Ejemplo de configuración:

```csharp
options.LogHttp.EnableBuffering = true;  // Por defecto
options.LogAudit.EnableBuffering = false; // Desactivado si hay middleware superior que ya gestiona buffering
```

> **Nota**: `LogJob` y `LogEndpoint` no tienen esta propiedad ya que no interactúan directamente con streams HTTP del pipeline de ASP.NET Core.

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

### 8.2 Modo "fire-and-forget" disparado desde un request

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

## 12. Manejo de excepciones controladas (Modo Legacy)

**Reec.Inspection** mantiene compatibilidad total con el sistema de excepciones del proyecto [**Reec**](https://github.com/edychumpitaz/Reec) original mediante `ReecException` y `ReecMessage`.

Este modo es útil cuando:
- Migras desde **Reec** a **Reec.Inspection**.
- Necesitas mantener contratos de respuesta existentes con clientes.
- Prefieres un formato de respuesta personalizado sobre RFC 7807 (ProblemDetails).

### 12.1. Configuración

Para usar el modo legacy, establece `EnableProblemDetails = false` (es el valor por defecto):

```csharp
builder.Services.AddReecInspection<InspectionDbContext>(
    options => options.UseSqlServer(connString),
    options =>
    {
        options.ApplicationName = "Legacy.Api";
        options.EnableProblemDetails = false; // Modo legacy activado
    });
```

### 12.2. Categorías de error disponibles

Las categorías están definidas en el enum `Category` y representan diferentes tipos de respuestas:

| Categoría | Valor | HTTP Status | Uso |
|-----------|-------|-------------|-----|
| `OK` | 200 | 200 | Operación exitosa |
| `PartialContent` | 206 | 206 | Consulta exitosa sin contenido |
| `Unauthorized` | 401 | 401 | Autenticación requerida |
| `Forbidden` | 403 | 403 | Sin permisos suficientes |
| `Warning` | 460 | 400 | Validación de campos |
| `BusinessLogic` | 465 | 400 | Errores controlados de negocio |
| `BusinessLogicLegacy` | 470 | 400 | Errores controlados de sistemas externos |
| `InternalServerError` | 500 | 500 | Errores no controlados |
| `BadGateway` | 502 | 502 | Error en sistema externo |
| `GatewayTimeout` | 504 | 504 | Timeout en sistema externo |

### 12.3. Formas de uso

#### a) Mensaje simple

```csharp
[HttpPost("create-user")]
public IActionResult CreateUser(CreateUserRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        throw new ReecException(Category.Warning, "El correo electrónico es obligatorio.");
    }

    // Lógica de creación...
    return Ok();
}
```

**Respuesta JSON:**
```json
{
  "id": 42,
  "path": "/create-user",
  "traceIdentifier": "0HNGTMGA752BQ:00000001",
  "category": 460,
  "categoryDescription": "Warning",
  "message": ["El correo electrónico es obligatorio."]
}
```

#### b) Lista de mensajes

```csharp
[HttpPost("validate-form")]
public IActionResult ValidateForm(FormData data)
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(data.Name))
        errors.Add("El nombre es obligatorio.");
    
    if (data.Age < 18)
        errors.Add("Debe ser mayor de 18 años.");
    
    if (errors.Any())
    {
        throw new ReecException(Category.Warning, errors);
    }

    return Ok();
}
```

**Respuesta JSON:**
```json
{
  "id": 43,
  "path": "/validate-form",
  "traceIdentifier": "0HNGTMGA752BQ:00000002",
  "category": 460,
  "categoryDescription": "Warning",
  "message": [
    "El nombre es obligatorio.",
    "Debe ser mayor de 18 años."
  ]
}
```

#### c) Error de lógica de negocio

```csharp
[HttpPost("transfer")]
public IActionResult Transfer(TransferRequest request)
{
    var account = GetAccount(request.AccountId);
    
    if (account.Balance < request.Amount)
    {
        throw new ReecException(
            Category.BusinessLogic, 
            "Saldo insuficiente para realizar la transferencia."
        );
    }

    // Procesar transferencia...
    return Ok();
}
```

**Respuesta JSON:**
```json
{
  "id": 44,
  "path": "/transfer",
  "traceIdentifier": "0HNGTMGA752BQ:00000003",
  "category": 465,
  "categoryDescription": "Business Logic",
  "message": ["Saldo insuficiente para realizar la transferencia."]
}
```

#### d) Captura de excepción con contexto

```csharp
[HttpPost("import-data")]
public IActionResult ImportData(ImportRequest request)
{
    try
    {
        var result = ExternalService.ProcessFile(request.FilePath);
        return Ok(result);
    }
    catch (Exception ex)
    {
        throw new ReecException(
            Category.BusinessLogicLegacy,
            "Error al procesar el archivo de importación.",
            ex.Message  // ExceptionMessage original
        );
    }
}
```

**Respuesta JSON:**
```json
{
  "id": 45,
  "path": "/import-data",
  "traceIdentifier": "0HNGTMGA752BQ:00000004",
  "category": 470,
  "categoryDescription": "Business Logic Legacy",
  "message": ["Error al procesar el archivo de importación."]
}
```

> **Nota**: En la base de datos (`LogHttp`), el campo `ExceptionMessage` contendrá el mensaje original de la excepción, mientras que `MessageUser` guarda el mensaje amigable para el cliente.

#### e) Excepción con InnerException

```csharp
[HttpGet("external-data")]
public async Task<IActionResult> GetExternalData()
{
    try
    {
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("/data");
        response.EnsureSuccessStatusCode();
        return Ok(await response.Content.ReadAsStringAsync());
    }
    catch (HttpRequestException ex)
    {
        throw new ReecException(
            Category.BadGateway,
            "No se pudo conectar con el servicio externo.",
            ex.Message,
            ex.InnerException  // Se preserva InnerException
        );
    }
}
```

### 12.4. Comportamiento del middleware

`LogHttpMiddleware` detecta automáticamente si la excepción es del tipo `ReecException`:

1. **Extrae** el `ReecMessage` configurado.
2. **Asigna** el `HttpStatusCode` según la categoría:
   - `Warning`, `BusinessLogic`, `BusinessLogicLegacy` → **400 Bad Request**
   - Otras categorías → Usan su código numérico directo.
3. **Persiste** el log en la tabla `LogHttp` (si está habilitado).
4. **Devuelve** el objeto `ReecMessage` serializado como JSON.

### 12.5. Ventajas del modo Legacy

✅ **Retrocompatibilidad**: Los clientes existentes siguen funcionando sin cambios.  
✅ **Flexibilidad**: Control total sobre la estructura de respuesta.  
✅ **Trazabilidad**: Cada error registra un `Id` único en base de datos.  
✅ **Claridad**: Las categorías personalizadas son más descriptivas que códigos HTTP estándar.

---

## 13. Manejo de excepciones con ProblemDetails (Modo Actual)

**Reec.Inspection** soporta el estándar **RFC 7807** (Problem Details for HTTP APIs) para respuestas de error estructuradas y consistentes.

Este modo es recomendado cuando:
- Construyes APIs nuevas siguiendo estándares de la industria.
- Necesitas interoperabilidad con frameworks y herramientas que esperan RFC 7807.
- Quieres aprovechar características de ASP.NET Core como `UseExceptionHandler`.

### 13.1. Configuración

Para activar el modo `ProblemDetails`, establece `EnableProblemDetails = true`:

```csharp
builder.Services.AddReecInspection<InspectionDbContext>(
    options => options.UseSqlServer(connString),
    options =>
    {
        options.ApplicationName = "Modern.Api";
        options.EnableProblemDetails = true;  // Modo actual activado
        options.InternalServerErrorMessage = "Error no controlado del sistema.";
    });
```

### 13.2. Estructura de respuesta

Cuando ocurre una excepción, la respuesta sigue el formato estándar RFC 7807:

```json
{
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Error no controlado del sistema.",
  "instance": "/api/demo/error",
  "id": 1,
  "category": 500,
  "traceIdentifier": "0HNGTMGA752BQ:00000003"
}
```

#### Campos de la respuesta

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `title` | string | Nombre legible de la categoría de error |
| `status` | int | Código HTTP estándar |
| `detail` | string | Mensaje de error descriptivo para el usuario |
| `instance` | string | Path del endpoint que generó el error |
| `id` | int | ID del registro en la tabla `LogHttp` (0 si no se guardó) |
| `category` | int | Código de categoría interno (compatible con modo legacy) |
| `traceIdentifier` | string | Identificador único para correlación de logs |

### 13.3. Ejemplo de captura automática

El middleware captura automáticamente cualquier excepción no controlada:

```csharp
[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    [HttpGet("InternalServerError")]
    public IActionResult InternalServerError()
    {
        var numerador = 1;
        var denominador = 0;
        var dividendo = numerador / denominador; // DivideByZeroException
        return Ok(dividendo);
    }
}
```

**Request:**
```
GET /api/demo/InternalServerError
```

**Response (500 Internal Server Error):**
```json
{
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Error no controlado del sistema.",
  "instance": "/api/demo/InternalServerError",
  "id": 1,
  "category": 500,
  "traceIdentifier": "0HNGTMGA752BQ:00000003"
}
```

### 13.4. Compatibilidad con ReecException

Aunque uses `ProblemDetails`, puedes seguir lanzando `ReecException` para controlar la categoría:

```csharp
[HttpPost("validate")]
public IActionResult Validate(UserInput input)
{
    if (string.IsNullOrWhiteSpace(input.Email))
    {
        throw new ReecException(
            Category.Warning, 
            "El correo electrónico es obligatorio."
        );
    }
    return Ok();
}
```

**Response (400 Bad Request):**
```json
{
  "title": "Warning",
  "status": 400,
  "detail": "El correo electrónico es obligatorio.",
  "instance": "/validate",
  "id": 2,
  "category": 460,
  "traceIdentifier": "0HNGTMGA752BQ:00000004"
}
```

### 13.5. Códigos de estado HTTP

El middleware mapea las categorías personalizadas a códigos HTTP estándar:

| Categoría | Código Interno | HTTP Status | Title |
|-----------|----------------|-------------|-------|
| `OK` | 200 | 200 | OK |
| `PartialContent` | 206 | 206 | Partial Content |
| `Unauthorized` | 401 | 401 | Unauthorized |
| `Forbidden` | 403 | 403 | Forbidden |
| `Warning` | 460 | **400** | Warning |
| `BusinessLogic` | 465 | **400** | Business Logic |
| `BusinessLogicLegacy` | 470 | **400** | Business Logic Legacy |
| `InternalServerError` | 500 | 500 | Internal Server Error |
| `BadGateway` | 502 | 502 | Bad Gateway |
| `GatewayTimeout` | 504 | 504 | Gateway Timeout |

> **Importante**: Las categorías `Warning`, `BusinessLogic` y `BusinessLogicLegacy` se traducen a **400 Bad Request** para cumplir con los estándares HTTP.

### 13.6. Detección de ProblemDetails

Todas las respuestas de error en modo `ProblemDetails` incluyen un header personalizado:

```
EnableProblemDetails: true
```

Esto permite a los clientes detectar automáticamente el formato de respuesta.

### 13.7. Persistencia en base de datos

Independientemente del formato de respuesta (Legacy o ProblemDetails), todos los errores se registran en la tabla `LogHttp` con:

- Timestamp con zona horaria configurada
- Stack trace completo
- Request body (si está habilitado buffering)
- Headers filtrados según configuración
- Categoría y mensaje de error
- Duración de la petición

### 13.8. Ventajas del modo ProblemDetails

✅ **Estándar RFC 7807**: Compatible con herramientas y bibliotecas de la industria.  
✅ **Integración nativa**: Funciona con `UseExceptionHandler` de ASP.NET Core.  
✅ **Extensibilidad**: Puedes agregar propiedades personalizadas en `Extensions`.  
✅ **Herramientas**: Swagger, Postman y otros clientes entienden el formato automáticamente.  
✅ **Trazabilidad**: Mantiene `traceIdentifier` para correlación con logs.

### 13.9. Comparación con el modo Legacy

| Aspecto | Modo Legacy | Modo ProblemDetails |
|---------|-------------|---------------------|
| Formato | `ReecMessage` personalizado | RFC 7807 estándar |
| Retrocompatibilidad | ✅ Con **Reec** original | ❌ Requiere actualizar clientes |
| Estándar industria | ❌ | ✅ |
| Categorías custom | ✅ 460, 465, 470 | ✅ Traducidas a 400 |
| Tooling support | ⚠️ Limitado | ✅ Amplio |
| Persistencia BD | ✅ | ✅ |
| TraceIdentifier | ✅ | ✅ |

---

## 14. Estado del proyecto

- Repositorio: [github.com/edychumpitaz/ReecInspection](https://github.com/edychumpitaz/ReecInspection)
- Proyecto Legacy: [github.com/edychumpitaz/Reec](https://github.com/edychumpitaz/Reec) (mantenido para referencia)
- Autor: **Edy Chumpitaz**
- Licencia: MIT
- Estado: **Desarrollo activo** 🚧
- Próximamente: Documentación completa estilo ReadTheDocs

### Roadmap

- [ ] Proveedores adicionales (PostgreSQL, MySQL, MongoDB, Oracle)
- [ ] Métricas y alertas configurables
- [ ] Dashboard de visualización de logs


### Contribuciones

¿Tienes ideas, sugerencias o encontraste un bug? 

- 🐛 Reporta issues: [GitHub Issues](https://github.com/edychumpitaz/ReecInspection/issues)
- 💡 Propón mejoras: [GitHub Discussions](https://github.com/edychumpitaz/ReecInspection/discussions)
- 🔀 Envía Pull Requests: Son bienvenidos siguiendo las convenciones del proyecto

> **Nota**: Este proyecto es una reescritura completa del proyecto original [**Reec**](https://github.com/edychumpitaz/Reec), con arquitectura mejorada, soporte para .NET 8+, y nuevas características como workers de limpieza, resiliencia HTTP y modos de respuesta intercambiables.

---

<div align="center">

**Construido con ❤️ para la comunidad .NET**

[⭐ Dale una estrella en GitHub](https://github.com/edychumpitaz/ReecInspection) si te fue útil

</div>
