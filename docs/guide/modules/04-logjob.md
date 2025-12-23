# LogJob — Ejecución y trazabilidad de Workers

`LogJob` permite **registrar la ejecución de procesos en segundo plano (workers/jobs)**, proporcionando trazabilidad homogénea, manejo consistente de errores y soporte nativo para limpieza automática.

Está diseñado para trabajar junto al componente **IWorker**, estandarizando el ciclo de vida de los jobs.

---

## 1. ¿Cuándo usar LogJob?

Usa `LogJob` cuando:

- Ejecutas **HostedServices** periódicos.
- Lanzas **jobs bajo demanda** (endpoint administrativo).
- Procesas **tareas batch**, sincronizaciones o limpiezas.
- Necesitas **auditoría y trazabilidad** de procesos no HTTP.

---

## 2. Campos compartidos (comunes a todos los módulos)

Estos campos existen en **LogAudit, LogHttp, LogEndpoint y LogJob** y son usados por los **workers de limpieza**.

| Propiedad | Descripción | Default |
|---|---|---|
| `IsSaveDB` | Habilita/deshabilita persistencia en base de datos. | `true` |
| `Schema` | Esquema donde se almacenará la tabla. | `null` |
| `TableName` | Nombre de la tabla del módulo. | `"LogJob"` |
| `EnableClean` | Habilita limpieza automática por retención. | `true` |
| `CronValue` | Expresión CRON para ejecutar la limpieza. | `"0 2 * * *"` |
| `DeleteDays` | Retención en días. | `10` |
| `DeleteBatch` | Tamaño de borrado por lote. | `100` |

### Índices recomendados

Para buen performance de limpieza:

- Índice por `CreateDateOnly`
- Índice compuesto `(ApplicationName, CreateDateOnly)` en escenarios multi-app

---

## 3. Parámetros específicos de LogJob

| Propiedad | Descripción | Default |
|---|---|---|
| `NameJob` | Nombre lógico del job ejecutado. | `null` |
| `CreateUser` | Usuario o sistema que inició la ejecución (API, sistema, admin, scheduler). | `null` |
| `TraceIdentifier` | Identificador de trazabilidad para correlacionar la ejecución del job con otros logs o requests. | `null` |
| `IsLightExecution` | Si es `true`, registra solo errores. Si es `false`, registra todo el ciclo. | `false` |
| `Delay` | Retraso opcional antes de ejecutar el job. Útil para escalonar procesos o evitar picos de carga. | `null` |

---

## 4. Relación con IWorker

`LogJob` **no se usa directamente**. Es gestionado automáticamente por **IWorker**.

Cada ejecución registra:

- Inicio del job
- Resultado (OK / Error)
- Duración
- Excepción (si aplica)
- Metadatos de ejecución

---

## 5. Limpieza automática de jobs

Ejemplo de configuración:

```csharp
options.LogJob.EnableClean = true;
options.LogJob.CronValue = "0 1 * * *"; // 1 a.m.
options.LogJob.DeleteDays = 15;
options.LogJob.DeleteBatch = 200;
```

La limpieza filtra por:

- `CreateDateOnly`
- `ApplicationName`

---

## 6. Buenas prácticas

- Usa nombres claros en `NameJob`.
- Prefiere `IsLightExecution = true` para jobs muy frecuentes.
- No hagas logging manual dentro del job.
- Centraliza la ejecución usando **IWorker**.

---

## 7. Relación con observabilidad

`LogJob` permite:

- Trazar fallos silenciosos.
- Auditar procesos batch.
- Correlacionar ejecuciones con errores de sistema.
- Analizar performance de jobs.
