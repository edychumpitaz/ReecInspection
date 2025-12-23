
# LogEndpoint — Integraciones HTTP salientes

`LogEndpoint` permite **auditar y trazar llamadas HTTP salientes** hacia sistemas externos (APIs, microservicios, ERPs, pagos, etc.), dejando evidencia completa de request, response, errores y tiempos de ejecución.

A diferencia de `LogHttp`, este módulo **no es middleware**: se usa de forma **explícita** al ejecutar una llamada externa o mediante el pipeline de resiliencia.

---

## 1. ¿Cuándo usar LogEndpoint?

Usa `LogEndpoint` cuando:

- Consumes **APIs de terceros**.
- Llamas **microservicios externos o remotos**.
- Integras sistemas legacy (ERP, CRM, pagos).
- Necesitas **trazabilidad contractual** de integraciones.

> La idea es envolver la llamada HTTP y dejar evidencia completa de la interacción.

---

## 2. Campos compartidos (comunes a todos los módulos)

Estos campos existen en **LogAudit, LogHttp, LogEndpoint y LogJob** y son usados por los **workers de limpieza**.

| Propiedad | Descripción | Default |
|---|---|---|
| `IsSaveDB` | Habilita/deshabilita persistencia en base de datos. | `true` |
| `Schema` | Esquema donde se almacenará la tabla. | `null` |
| `TableName` | Nombre de la tabla del módulo. | `"LogHttp"` |
| `EnableClean` | Habilita limpieza automática por retención. | `true` |
| `CronValue` | Expresión CRON para ejecutar la limpieza. | `"0 2 * * *"` |
| `DeleteDays` | Retención en días. | `10` |
| `DeleteBatch` | Tamaño de borrado por lote. | `100` |

### Índices recomendados

Para buen performance de limpieza:

- Índice por `CreateDateOnly`
- Índice compuesto `(ApplicationName, CreateDateOnly)` en escenarios multi-app

---


## 3. Resiliencia HTTP (AddReecInspectionResilience)

`AddReecInspectionResilience` integra automáticamente:

- Registro con **LogEndpoint**
- Retry con backoff
- Timeout
- Circuit breaker

Basado en `Microsoft.Extensions.Http.Resilience` (Polly).

### 3.1 Registro en `Program.cs`

```csharp
var httpBuilder = builder.Services.AddHttpClient("PlaceHolder", httpClient =>
{
    httpClient.DefaultRequestHeaders.Clear();
    httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
});

builder.Services.AddReecInspectionResilience(httpBuilder);
```

Esto **no requiere configuración adicional** para comenzar a registrar integraciones.

---

### 3.2 Uso de un Controller con IHttpClientFactory

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

✔ La llamada queda:
- Registrada en `LogEndpoint`
- Protegida con resiliencia
- Correlacionada por `TraceIdentifier`

---

## 4. Limpieza automática

Ejemplo de limpieza para integraciones:

```csharp
options.LogEndpoint.EnableClean = true;
options.LogEndpoint.CronValue = "0 3 * * *"; // 3 a.m.
options.LogEndpoint.DeleteDays = 10;
options.LogEndpoint.DeleteBatch = 100;
```

La limpieza filtra por:

- `CreateDateOnly`
- `ApplicationName` (si está configurado)

