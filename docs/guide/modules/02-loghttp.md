# LogHttp (Errores)

`LogHttp` es el módulo de Reec.Inspection orientado a **capturar errores HTTP** y la evidencia necesaria para diagnosticarlos.
Su foco es: **cuando algo falla**, dejar trazabilidad del request/response, metadatos y excepción.

> Si buscas auditoría funcional (request + response en operaciones exitosas), usa `LogAudit`.  
> Si buscas jobs en segundo plano, usa `LogJob`.  
> `LogHttp` es tu “caja negra” para errores en el pipeline HTTP.

---

##  Antes de empezar (dos conceptos clave)

### `ApplicationName` (global)

Reec.Inspection puede convivir con **múltiples aplicaciones** compartiendo la misma base de datos.
El campo **`ApplicationName`** existe en las 4 tablas principales y se usa para:

- Separar logs por sistema/app.
- Soportar borrado/retención por aplicación.
- Mantener ReecInspection en una BD “central” si lo necesitas.

Configúralo una vez en las opciones globales:

```csharp
builder.Services.AddReecInspection(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
    {
        inspection.ApplicationName = "Resemin.Quality.Api"; // tu nombre de app
    });
```

> Recomendación: usa un nombre estable (no incluyas versión ni environment en el nombre).

### Limpieza y performance: `CreateDateOnly`

El proceso de retención/limpieza filtra por **`CreateDateOnly`** (campo presente en los 4 módulos).
Para que el borrado sea eficiente:

- Asegura índice por `CreateDateOnly`.
- Si manejas múltiples apps, considera índice compuesto: `(ApplicationName, CreateDateOnly)`.

---

## 1. Instalación

### 1.1 Paquetes NuGet

```bash
dotnet add package Reec.Inspection
dotnet add package Reec.Inspection.SqlServer
```

> Ajusta el provider según tu BD. En esta guía usamos SQL Server.

---

## 2. Configuración básica (Program.cs)

### 2.1 Registro del provider y opciones globales

```csharp
builder.Services.AddReecInspection(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
    {
        // Identidad de la app (clave para separar logs)
        inspection.ApplicationName = "Resemin.Quality.Api";

        // Recomendación: schema dedicado para logs
        inspection.LogHttp.Schema = "Inspection";
        inspection.LogHttp.TableName = "LogHttp";

        // Limpieza automática
        inspection.LogHttp.EnableClean = true;
        inspection.LogHttp.CronValue = "0 3 * * *"; // 3:00 a.m.
        inspection.LogHttp.DeleteDays = 15;
        inspection.LogHttp.DeleteBatch = 500;
    });
```

> Performance: El borrado se basa en `CreateDateOnly` y puede filtrarse también por `ApplicationName`.  
> Índices recomendados: `CreateDateOnly` y/o `(ApplicationName, CreateDateOnly)`.

---

## 3. Registrar el middleware

Agrega el middleware al pipeline HTTP.

```csharp
var app = builder.Build();

app.UseReecInspection();

app.MapControllers();
app.Run();
```

> Ubícalo **antes** de `MapControllers()` (o minimal endpoints) para asegurar captura global.

---

## 4. Control de headers (Include/Exclude)

LogHttp permite controlar qué headers se guardan para evitar:

- Guardar secretos (Authorization, cookies, etc.).
- Guardar demasiado ruido.
- Problemas de cumplimiento (PII).

### 4.1 Excluir headers sensibles (`HeaderKeysExclude`)

Recomendado como línea base:

```csharp
builder.Services.AddReecInspection(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
    {
        inspection.LogHttp.HeaderKeysExclude = new List<string>
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-Api-Key"
        };
    });
```

### 4.2 Incluir solo una lista específica (`HeaderKeysInclude`)

Si tu escenario es estricto, usa allow-list:

```csharp
builder.Services.AddReecInspection(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
{
    inspection.LogHttp.HeaderKeysInclude = new List<string>
    {
        "User-Agent",
        "X-Request-Id",
        "X-Forwarded-For"
    };
});
```

> Regla: cuando defines `HeaderKeysInclude`, estás diciendo “solo estos”.  
> Úsalo en producción si quieres minimizar riesgo.

---

## 5. IP real y RequestId desde headers

En ambientes con proxy / ingress / gateway, la IP real y el request id suelen venir en headers.

### 5.1 IP desde header (`IpAddressFromHeader`)

```csharp
builder.Services.AddReecInspection(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
{
    inspection.LogHttp.IpAddressFromHeader = "X-Forwarded-For";
});
```

> Si tu infraestructura usa otro header (ej. `X-Real-IP`), configúralo aquí.

### 5.2 RequestId desde header (`RequestIdFromHeader`)

```csharp
builder.Services.AddReecInspection(
     db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
{
    inspection.LogHttp.RequestIdFromHeader = "X-Request-Id";
});
```

> Recomendación: si ya tienes un CorrelationId corporativo, mapea ese header aquí.

---

## 6. `EnableBuffering` (cuándo activarlo)

Para leer el body del request, el pipeline puede requerir buffering.

```csharp
builder.Services.AddReecInspection(
     db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
{
    inspection.LogHttp.EnableBuffering = true;
});
```

Cuándo **sí**:
- APIs JSON típicas (payloads moderados).
- Necesitas cuerpo del request para diagnosticar errores.

Cuándo **no** (o excluir rutas):
- Upload de archivos (multipart grandes).
- Streams grandes (por memoria/IO).

> Recomendación: si tienes endpoints de upload, exclúyelos del logging o limita el body size (lo documentaremos en el módulo de configuración avanzada si aplica).

---

## 7. Limpieza automática (retención)

LogHttp soporta limpieza automática con:

- `EnableClean`
- `CronValue`
- `DeleteDays`
- `DeleteBatch`

Ejemplo recomendado:

```csharp
builder.Services.AddReecInspection(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
{
    inspection.ApplicationName = "Resemin.Quality.Api";

    inspection.LogHttp.EnableClean = true;
    inspection.LogHttp.CronValue = "0 3 * * *";
    inspection.LogHttp.DeleteDays = 15;
    inspection.LogHttp.DeleteBatch = 500;
});
```

### Índices recomendados (SQL Server)

Para mejorar el rendimiento de borrado por retención:

- Índice por `CreateDateOnly` (campo de filtro).
- Si compartes BD entre apps: índice compuesto `(ApplicationName, CreateDateOnly)`.

> Nota: la limpieza filtra por `CreateDateOnly` y puede acotar por `ApplicationName` para soportar múltiples sistemas.

---

## 8. Resultado esperado

Cuando ocurre un error HTTP:

- El error es capturado automáticamente.
- Se registra un log en la base de datos (tabla de `LogHttp`).
- No necesitas escribir logging manual para cada excepción.
- Si usas `ApplicationName`, podrás filtrar por sistema.

---

## 9. Checklist rápido

- [ ] Definiste `ApplicationName` (clave para multi-app en una misma BD).
- [ ] Registraste `UseReecInspection()` en el pipeline.
- [ ] Excluiste headers sensibles (`HeaderKeysExclude`) o usaste allow-list (`HeaderKeysInclude`).
- [ ] Configuraste `IpAddressFromHeader` y `RequestIdFromHeader` si tienes gateway/proxy.
- [ ] Habilitaste retención y limpieza (DeleteDays/DeleteBatch).
- [ ] Aseguraste índices por `CreateDateOnly` (y opcional `(ApplicationName, CreateDateOnly)`).
