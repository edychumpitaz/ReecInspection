# ADR-05 — Ejecución segura y trazable de jobs en background (LogJob / IWorker)

## Estado
Aceptado

---

## Contexto

En aplicaciones empresariales es común ejecutar procesos en background:

- limpieza y retención de logs
- sincronizaciones
- colas / reintentos
- tareas programadas (cron)
- mantenimiento de datos

En estos escenarios suele pasar que:

- cada equipo implementa su propia forma de ejecutar jobs
- el manejo de excepciones no es consistente
- no se registra trazabilidad (qué se ejecutó, cuándo, cuánto tardó, si falló)
- la observabilidad queda incompleta o depende del desarrollador

Además, muchos jobs se ejecutan con alta frecuencia, y registrar *todo* su ciclo
puede generar:

- alto volumen de datos en base de datos
- overhead de CPU / IO
- crecimiento innecesario de tablas

ReecInspection busca un patrón estándar para ejecutar jobs que:
- sea seguro ante fallos
- tenga trazabilidad consistente
- controle el costo de persistencia

---

## Decisión

Se decide implementar un modelo estándar para ejecución de jobs mediante:

- La interfaz **IWorker** como contrato de ejecución.
- La entidad **LogJob** como registro persistible de ejecución.
- La implementación **Worker** como envoltorio (wrapper) que centraliza:
  - ejecución
  - captura de excepciones
  - trazabilidad
  - persistencia controlada

Adicionalmente, se incorpora el modo **IsLightExecution** para permitir
ejecuciones “livianas” donde **solo se persisten fallos**, evitando overhead
en jobs frecuentes.

---

## Alternativas consideradas

### 1) Ejecutar jobs con `try/catch` manual por cada implementación
- ❌ Repetición de código
- ❌ Inconsistencia en manejo de errores
- ❌ Falta de trazabilidad uniforme
- ❌ Difícil de mantener y auditar

### 2) Frameworks completos de scheduling/processing (obligatorio)
- ⚠️ Dependencias y complejidad adicional
- ⚠️ No siempre aplicable a todos los entornos
- ⚠️ Menor control fino sobre persistencia y modelo de datos

### 3) Wrapper estándar + contrato IWorker (elegida)
- ✅ Consistencia en ejecución y errores
- ✅ Trazabilidad homogénea
- ✅ Control explícito del costo (modo light)
- ✅ Bajo acoplamiento (el consumidor decide cómo orquestar la ejecución)

---

## Detalles de implementación

### IWorker
Define un contrato para ejecutar lógica en background de manera estándar:

- `RunFunction(...)`: lógica principal del job
- `RunFunctionException(...)`: lógica de manejo/registro en caso de error
- `IsLightExecution`: define si la ejecución debe ser “liviana”

### LogJob
Entidad persistible para registrar ejecución de jobs, incluyendo estados:

- `Enqueued`
- `Processing`
- `Succeeded`
- `Failed`

y metadata de ejecución (tiempos, error, stack trace u otros campos según modelo).

### Worker (wrapper)
Centraliza:
- transición de estados (enqueue → processing → success/fail)
- medición de tiempo de ejecución
- captura de excepciones
- persistencia configurada por:
  - `EnableGlobalDbSave`
  - flags por entidad/módulo (por ejemplo `LogJob.IsSaveDB`)

### Modo IsLightExecution
Cuando `IsLightExecution = true`:
- No se registra el ciclo completo del job.
- Se persiste únicamente si ocurre una excepción (Failed).
- Se reduce el volumen de datos y el overhead.

### Correlación (TraceIdentifier)
El sistema utiliza `TraceIdentifier` como identificador de correlación para agrupar
y rastrear la ejecución del job.

- Si el job se origina desde una petición HTTP, se reutiliza el `TraceIdentifier`
  del request (ASP.NET Core) y se asigna a `LogJob.TraceIdentifier`.
- Si el job se origina en background (sin request), se genera un `GUID` y se
  asigna a `LogJob.TraceIdentifier` para correlacionar los estados:
  `Enqueued → Processing → Succeeded/Failed`.

Esto permite trazabilidad consistente tanto en escenarios request-driven como
en ejecuciones autónomas.


---

## Consideraciones de seguridad y privacidad (PII)

- LogJob puede contener mensajes de error, stack traces o datos contextuales.
- No se aplica masking automático.
- Se recomienda no persistir datos sensibles en mensajes de error.
- La responsabilidad de sanitización se delega al consumidor.

---

## Consecuencias

### Positivas
- Ejecución estandarizada y repetible para cualquier job.
- Manejo de errores consistente.
- Trazabilidad y diagnóstico mejorados.
- Control del costo de persistencia mediante modo “light”.
- Facilita la implementación de jobs de limpieza (CleanLog*Worker).

### Negativas / trade-offs
- Persistir el ciclo completo puede generar alto volumen en jobs frecuentes.
- Requiere disciplina para definir correctamente qué jobs deben ser “light”.
- Si el modo light se abusa, se pierde visibilidad de ejecuciones exitosas.

---

## Extensibilidad

El patrón IWorker/Worker permite:

- Correlación consistente mediante `TraceIdentifier` (request-driven o GUID).
- Métricas base ya disponibles (por ejemplo, duración), y posibilidad de ampliar a:
  contadores, tasa de fallos, percentiles o agregaciones por tipo de job.
- Incorporar políticas de reintento o backoff (si se decide a nivel de orquestación).
- Integración con schedulers externos sin cambiar el contrato.


---

## Implicaciones para otros módulos

- Los workers de limpieza dependen directamente de este patrón.
  Actualmente existen workers de limpieza para las tablas principales de logging
  (LogHttp, LogAudit, LogEndpoint y LogJob), lo que permite controlar retención y costo.
- LogHttp/LogAudit/LogEndpoint generan datos que requieren retención; LogJob facilita
  ejecutar y auditar limpieza programada.
- La estrategia de persistencia global impacta este módulo.


---

## Notas finales

LogJob / IWorker define un estándar de ejecución para background jobs en ReecInspection:
seguro, trazable y con control explícito de costos de almacenamiento.

Cualquier cambio que rompa este contrato o su semántica debe evaluarse mediante un nuevo ADR.
