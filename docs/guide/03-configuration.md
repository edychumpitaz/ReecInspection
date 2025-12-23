# Configuración

Reec.Inspection centraliza su configuración mediante opciones globales y opciones por módulo.
El objetivo es controlar **persistencia**, **limpieza**, **captura de contenido** y **comportamiento del middleware** sin tocar código de negocio.

---

## 1. Configuración base (Program.cs)

Ejemplo mínimo recomendado:

```csharp
builder.Services.AddReecInspection<InspectionDbContext>(
    db => db.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    inspection =>
    {
        inspection.ApplicationName = "Reec.Inspection.Api";       // Obligatorio
        inspection.SystemTimeZoneId = "SA Pacific Standard Time"; // Recomendado
        inspection.EnableProblemDetails = true;                   // Opcional
    });
```

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
| `CronValue` | Expresión CRON que define cuándo se ejecuta la limpieza. | `"0 2 * * *"` |
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
builder.Services.AddReecInspection(
    db => db.UseSqlServer(configuration.GetConnectionString("default")),
    inspection =>
    {
        //General
        inspection.ApplicationName = "Reec.Inspecion.Api";
        inspection.EnableMigrations = false;
        inspection.EnableProblemDetails = true;
        inspection.SystemTimeZoneId = "SA Pacific Standard Time";

        //Por modulo : LogAudit
        inspection.LogAudit.IsSaveDB = true;
        inspection.LogAudit.EnableClean = true;
        inspection.LogAudit.CronValue = "0 2 * * *"; // 2:00 a.m.
        inspection.LogAudit.DeleteDays = 10;
        inspection.LogAudit.DeleteBatch = 500;
        inspection.LogAudit.Schema = "Inspection";
        inspection.LogAudit.TableName = "LogAudit";
    });

```

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

---

## 5. Reglas recomendadas para producción

- Deshabilitar migraciones automáticas (`EnableMigrations = false`) y aplicar migraciones por pipeline.
- Definir retención y limpieza por módulo (evita crecimiento infinito).
- Evitar capturar contenido sensible (configurar exclusiones en LogHttp cuando aplique).
- Usar `ApplicationName` consistente para correlación entre ambientes y servicios.
