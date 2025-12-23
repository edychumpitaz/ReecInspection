# Configuración

Reec.Inspection centraliza su configuración mediante opciones globales y opciones por módulo.
El objetivo es controlar **persistencia**, **limpieza**, **captura de contenido** y **comportamiento del middleware** sin tocar código de negocio.

---

## 1. Configuración base (Program.cs)

Ejemplo mínimo recomendado:

```csharp
builder.Services.AddReecInspection<DbContextSqlServer>(
    db => db.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    options =>
    {
        options.ApplicationName = "Reec.Inspection.Api";       // Obligatorio
        options.SystemTimeZoneId = "SA Pacific Standard Time"; // Recomendado
        options.EnableProblemDetails = true;                   // Opcional
        options.EnableGlobalDbSave = true;                     // Recomendado
    });
```

`AddReecInspection`:

- Registra el `DbContextSqlServer` derivado de `InspectionDbContext` con `DbContextPool`.
- Registra middlewares (`LogAuditMiddleware`, `LogHttpMiddleware`).
- Registra `IWorker`, `IDateTimeService` y workers de limpieza (`CleanLog*Worker`) según configuración.
- Opcionalmente agrega soporte `ProblemDetails`.

---

## 2. Opciones globales (`ReecExceptionOptions`)

`ReecExceptionOptions` centraliza la configuración global del framework.

### Propiedades principales

| Propiedad | Descripción | Default |
|----------|-------------|---------|
| `ApplicationName` | Nombre de la aplicación que genera los logs. **Obligatorio** para identificar el origen. | `null` |
| `ApplicationErrorMessage` | Mensaje mostrado cuando ocurre un error al intentar guardar información en la base de datos. | `"Ocurrió un error al guardar log en Base de Datos."` |
| `InternalServerErrorMessage` | Mensaje genérico utilizado para errores internos del sistema. | `"Error no controlado del sistema."` |
| `SystemTimeZoneId` | Zona horaria usada para registrar fechas y programar tareas CRON. | `"SA Pacific Standard Time"` |
| `EnableMigrations` | Ejecuta migraciones automáticas al iniciar la aplicación. | `true` |
| `EnableProblemDetails` | Devuelve respuestas de error en formato **ProblemDetails (RFC 7807)**. | `false` |
| `EnableGlobalDbSave` | Habilita o deshabilita la escritura global en base de datos. | `true` |
| `MinCategory` | Categoría mínima de eventos a registrar. | `Unauthorized (401)` |

> Recomendación: En ambientes productivos, desactivar `EnableMigrations` y manejar migraciones mediante pipelines de CI/CD.


---

## 3. Configuración por módulo (visión general)

Cada módulo puede habilitar o deshabilitar persistencia, definir su almacenamiento y controlar políticas de limpieza de datos.

### Módulos disponibles

- **LogAudit**: auditoría funcional que registra el *input* recibido por la aplicación y el *response* devuelto por el sistema.
- **LogHttp**: captura de requests HTTP entrantes, responses y errores no controlados.
- **LogEndpoint**: trazabilidad de llamadas hacia servicios externos (integraciones).
- **LogJob**: ejecución y errores de procesos en segundo plano (jobs / workers).

---

### Parámetros comunes por módulo

| Propiedad | Descripción | Default |
|---------|-------------|---------|
| `IsSaveDB` | Habilita o deshabilita la persistencia del módulo en base de datos. | `true` |
| `Schema` | Esquema de base de datos donde se almacenan los registros. | `null` |
| `TableName` | Nombre de la tabla de almacenamiento del módulo. | *(depende del módulo)* |
| `EnableClean` | Habilita la limpieza automática de registros antiguos. | `true` |
| `CronValue` | Expresión CRON que define cuándo se ejecuta la limpieza. Puedes usar [https://crontab.guru](https://crontab.guru) para generar y validar expresiones CRON.| `"0 2 * * *"` |
| `DeleteDays` | Retención de registros en días antes de ser eliminados. | `10` |
| `DeleteBatch` | Cantidad de registros eliminados por lote. | `100` |


### Valores por defecto de tablas

| Módulo | TableName |
|------|-----------|
| LogAudit | `LogAudit` |
| LogHttp | `LogHttp` |
| LogEndpoint | `LogEndpoint` |
| LogJob | `LogJob` |


### Ejemplo de configuración desde Program.cs

```csharp
builder.Services.AddReecInspection<DbContextSqlServer>(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    options =>
    {
        //General
        options.ApplicationName = "Reec.Inspecion.Api";
        options.EnableMigrations = false;
        options.EnableProblemDetails = true;
        options.SystemTimeZoneId = "SA Pacific Standard Time";

        //Por modulo : LogAudit
        options.LogAudit.IsSaveDB = true;
        options.LogAudit.EnableClean = true;
        options.LogAudit.CronValue = "0 2 * * *"; // 2:00 a.m.
        options.LogAudit.DeleteDays = 10;
        options.LogAudit.DeleteBatch = 500;
        options.LogAudit.Schema = "Inspection";
        options.LogAudit.TableName = "LogAudit";
    });

```

Aplica el mismo patrón para LogHttp, LogJob y LogEndpoint.

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

## 4. Orden recomendado de middlewares

Un orden típico para mantener performance y consistencia:

```csharp
var app = builder.Build();

app.UseResponseCompression();
app.UseReecInspection();
app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

## 5. Importancia de `SystemTimeZoneId`

Todas las fechas registradas en los logs y workers usan esta zona horaria:

- Fechas de creación.
- Ejecuciones de jobs.
- Cálculo de `CronValue`. Puedes usar [https://crontab.guru](https://crontab.guru) para generar y validar expresiones CRON.

```csharp
options.SystemTimeZoneId = "SA Pacific Standard Time";
```

Para ver las zonas disponibles:

```csharp
var zones = TimeZoneInfo.GetSystemTimeZones();
```

Si el ID es inválido, la inicialización de `IDateTimeService` lanzará excepción.

---

## 6. Registro de `ApplicationName`

Obligatorio para distinguir qué sistema originó cada registro.

```csharp
options.ApplicationName = "Billing.Api";
```

Se utiliza en todas las tablas de log como columna de referencia.

---

## 7. Guardado condicional

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

## 8. Reglas recomendadas para producción

- Deshabilitar migraciones automáticas (`EnableMigrations = false`) y aplicar migraciones por pipeline.
- Definir retención y limpieza por módulo (evita crecimiento infinito).
- Evitar capturar contenido sensible (configurar exclusiones en LogHttp cuando aplique).
- Usar `ApplicationName` consistente para correlación entre ambientes y servicios.
