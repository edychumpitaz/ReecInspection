# ADR-06 — CorrelationContext: TraceIdentifier + RequestId (propagación y herencia)

## Estado
Pendiente (En diseño)

---

## Contexto

ReecInspection maneja distintos tipos de ejecución:

- Peticiones HTTP entrantes (LogHttp, LogAudit)
- Jobs en background (LogJob / IWorker)
- Llamadas HTTP salientes (LogEndpoint)

Actualmente:

- En peticiones HTTP se dispone de:
  - `TraceIdentifier` (identificador interno del request)
  - `RequestId` (header externo, por ejemplo `X-Correlation-Id`)
- En jobs en background, se genera un identificador propio (GUID).
- LogEndpoint no siempre puede correlacionar llamadas salientes cuando
  no existe un contexto HTTP activo.

Esto genera la necesidad de una **estrategia unificada de correlación**
que funcione tanto en escenarios request-driven como background-driven,
sin acoplar la librería al uso obligatorio de `IHttpContextAccessor`.

---

## Problema

- La correlación de logs depende actualmente del origen de ejecución.
- En ejecuciones sin request, no existe una fuente común de TraceIdentifier
  accesible para todos los módulos.
- Obligar al consumidor a registrar `IHttpContextAccessor` introduce
  acoplamiento innecesario al mundo HTTP.
- Se requiere una forma de:
  - propagar identificadores
  - heredar contexto
  - mantener trazabilidad consistente

---

## Decisión (Propuesta)

Definir un **CorrelationContext** centralizado que permita:

- Almacenar el contexto de correlación actual por ejecución.
- Manejar dos identificadores con roles distintos:
  - **TraceIdentifier**: correlación interna obligatoria.
  - **RequestId**: correlación externa opcional (si existe).
- Propagar automáticamente estos valores entre:
  - middlewares
  - workers
  - handlers HTTP salientes
- Evitar la dependencia obligatoria de `IHttpContextAccessor`.

La obtención del identificador podrá:
- reutilizar valores existentes (request)
- generar valores propios (GUID)
- o delegarse a una función configurable definida por el consumidor.

---

## Reglas de correlación (intención)

- **TraceIdentifier**
  - Siempre debe existir en logs internos.
  - Es el identificador principal de auditoría interna.
- **RequestId**
  - Es opcional.
  - Se hereda solo si existe un contexto de request.
  - Permite integración con gateways, balanceadores y sistemas externos.

---

## Alcance previsto

El CorrelationContext será utilizado por:

- LogHttp
- LogAudit
- LogJob / IWorker
- LogEndpoint

Garantizando que:
- los registros internos puedan correlacionarse entre sí
- la integración con observabilidad externa sea opcional y no invasiva

---

## Consideraciones futuras

Este diseño permitirá, en el futuro:

- Integración transparente con OpenTelemetry (`Activity`, `TraceId`)
- Desactivación de persistencia en BD y envío directo a sistemas externos
- Correlación distribuida sin cambiar contratos existentes

---

## Notas finales

Este ADR define una **intención arquitectónica**, no una implementación inmediata.

La implementación concreta, contratos y lifetimes del CorrelationContext
se definirán en un ADR posterior de construcción.
