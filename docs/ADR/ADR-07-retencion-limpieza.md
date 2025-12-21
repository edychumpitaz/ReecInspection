# ADR-07 — Retención y limpieza de logs (CleanLog*Worker)

## Estado
Aceptado

---

## Contexto

Reec.Inspection persiste información de observabilidad en base de datos
(LogHttp, LogAudit, LogEndpoint, LogJob) como primer nivel operativo.

En producción, este enfoque implica un crecimiento constante de datos,
especialmente en escenarios de:

- alto tráfico HTTP
- múltiples integraciones externas
- jobs recurrentes
- errores repetitivos

Sin una estrategia explícita de retención y limpieza:

- el tamaño de la base de datos crece sin control
- se incrementan costos de almacenamiento y mantenimiento
- se degrada el performance de consultas
- la observabilidad mínima deja de ser viable

Adicionalmente, Reec.Inspection permite configurar un `ApplicationName`
explícito, el cual se persiste junto a cada registro de observabilidad.

Esto habilita escenarios donde múltiples aplicaciones o servicios
almacenan sus logs en una base de datos centralizada de observabilidad,
sin impactar la base de datos de negocio de cada sistema.

---

## Problema

- No todos los registros necesitan conservarse indefinidamente.
- La limpieza manual es propensa a errores y olvidos.
- Ejecutar eliminaciones ad-hoc desde el código de negocio introduce riesgo.
- La limpieza debe ser:
  - automática
  - trazable
  - controlada
  - segura ante fallos

---

## Decisión

Se adopta una estrategia explícita de **retención y limpieza automática**
mediante **workers dedicados**, utilizando el patrón `IWorker / LogJob`.

Para cada tipo de log persistido se define un worker de limpieza específico:

- `CleanLogHttpWorker`
- `CleanLogAuditWorker`
- `CleanLogEndpointWorker`
- `CleanLogJobWorker`

Cada worker es responsable de:

- eliminar registros antiguos según una política de retención configurada
- ejecutar la limpieza en lotes controlados
- registrar su ejecución mediante `LogJob`
- fallar de forma segura sin afectar el sistema principal

La estrategia de limpieza se aplica siempre de forma **aislada por aplicación**,
utilizando la propiedad `ApplicationName` como criterio obligatorio de segmentación.

Ningún worker de limpieza elimina información de otras aplicaciones que
compartan la misma base de datos de observabilidad.

---

## Alternativas consideradas

### 1) No limpiar (retención infinita)
- ❌ Crecimiento ilimitado de BD
- ❌ Riesgo operativo y de costos
- ❌ No viable en producción

### 2) Limpieza manual o scripts externos
- ❌ Dependencia operativa
- ❌ Difícil de auditar
- ❌ Propenso a errores humanos

### 3) Limpieza automática con workers dedicados (elegida)
- ✅ Automatización consistente
- ✅ Trazabilidad completa (LogJob)
- ✅ Control de impacto (batching)
- ✅ Integración natural con el modelo existente

---

## Detalles de implementación

### Política de retención

Cada worker aplica una política basada en:

- cantidad de días de retención (por ejemplo: 7, 15, 30, 90 días)
- configuración por módulo
- entorno (DEV / QA / PRD)

### Ejecución por lotes

La eliminación se realiza en **batches** para evitar:

- locks prolongados
- consumo excesivo de CPU/RAM
- impacto negativo en el sistema productivo

### Orquestación

- Los workers pueden ejecutarse mediante:
  - jobs programados (cron)
  - schedulers externos
  - procesos en background del host
- La librería no impone un scheduler específico.

### Segmentación por ApplicationName

Cada registro persistido por Reec.Inspection incluye la propiedad
`ApplicationName`, configurada desde las opciones de la librería.

Los workers de limpieza (`CleanLog*Worker`):

- filtran siempre por `ApplicationName`
- aplican la política de retención solo a los registros de la aplicación actual
- permiten que múltiples aplicaciones compartan una misma base de datos
  de observabilidad sin interferencia entre ellas

Esta segmentación habilita una base de datos de logs centralizada,
independiente de la base de datos de negocio de cada aplicación.

---

## Integración con LogJob / IWorker

- Cada ejecución de limpieza se registra como un `LogJob`.
- Se capturan:
  - estado (Succeeded / Failed)
  - duración
  - errores (si existen)
- Se recomienda ejecutar estos workers en **modo `IsLightExecution`**:
  - solo se persisten fallos
  - se reduce el volumen de logs de limpieza

---

## Consideraciones de seguridad y privacidad (PII)

- La limpieza elimina información potencialmente sensible almacenada
  en logs antiguos.
- La retención debe alinearse con políticas internas o regulatorias
  del consumidor (por ejemplo GDPR).
- Reec.Inspection no impone valores por defecto legales; delega la
  responsabilidad de configuración al consumidor.

---

## Consecuencias

### Positivas
- Control explícito del crecimiento de la base de datos.
- Observabilidad sostenible en el tiempo.
- Automatización sin intervención manual.
- Trazabilidad de las tareas de mantenimiento.
- Permite centralizar la observabilidad de múltiples aplicaciones en una
  sola base de datos, manteniendo aislamiento lógico por `ApplicationName`.


### Negativas / trade-offs
- Eliminaciones frecuentes generan carga adicional (mitigada por batching).
- Retención muy agresiva puede eliminar información útil para diagnóstico.
- Requiere configuración consciente por entorno.

---

## Relación con otros ADRs

- **ADR-01**: habilita la operación mínima en BD.
- **ADR-05**: LogJob/IWorker provee ejecución segura y trazable.
- **ADR-06**: la persistencia por módulo y el uso de `ApplicationName`
  permiten separar la base de datos de observabilidad de la base de negocio.

- **ADR-30 (pendiente)**: la correlación permite rastrear limpiezas
  asociadas a ejecuciones previas.

---

## Notas finales

La retención y limpieza no es un detalle operativo, sino un componente
esencial del diseño de Reec.Inspection.

Sin una estrategia explícita de limpieza, la observabilidad mínima en BD
no es sostenible en producción.

Cualquier cambio en políticas de retención o estrategia de limpieza
debe evaluarse mediante un nuevo ADR.
