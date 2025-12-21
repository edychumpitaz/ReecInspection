# ADR-04 — Trazabilidad de llamadas HTTP salientes (LogEndpoint)

## Estado
Aceptado

---

## Contexto

En aplicaciones empresariales modernas, las APIs no operan de forma aislada.
Con frecuencia realizan llamadas HTTP hacia:

- servicios internos
- APIs de terceros
- sistemas legacy
- integraciones externas

Cuando ocurre un problema en estos escenarios, suele ser difícil responder:

- ¿a qué servicio se llamó?
- ¿cuánto tardó?
- ¿qué status code devolvió?
- ¿el fallo fue interno o externo?

La falta de trazabilidad en llamadas salientes complica:
- diagnóstico de errores
- análisis de performance
- soporte en producción

---

## Decisión

Se decide implementar **LogEndpoint** mediante un
`DelegatingHandler` (`LogEndpointHandler`) que intercepte
todas las llamadas HTTP realizadas con `HttpClient`.

LogEndpoint:

- Registra llamadas HTTP **salientes**
- Es independiente del pipeline de entrada (middlewares)
- Se integra de forma transparente mediante `HttpClientFactory`

---

## Alternativas consideradas

### 1) Logging manual por cada llamada HTTP
- ❌ Código repetitivo
- ❌ Alta probabilidad de omisiones
- ❌ Difícil de mantener consistente

### 2) Instrumentación externa obligatoria
- ⚠️ Dependencia de herramientas externas
- ⚠️ Menor control del formato y persistencia
- ⚠️ No siempre disponible en todos los entornos

### 3) DelegatingHandler centralizado (elegida)
- ✅ Transparente para el desarrollador
- ✅ Aplicable a todas las llamadas HTTP
- ✅ Integrable con `HttpClientFactory`
- ✅ Bajo acoplamiento

---

## Detalles de implementación

- LogEndpoint se implementa como `DelegatingHandler`.
- Intercepta cada request/response saliente capturando:
  - método HTTP
  - URL / endpoint
  - status code
  - duración de la llamada
- Se integra mediante:
  - registro explícito en `HttpClientFactory`
- El guardado en base de datos es configurable mediante:
  - `EnableGlobalDbSave`
  - opciones específicas del módulo LogEndpoint

---

## Consideraciones de seguridad y privacidad (PII)

Actualmente, LogEndpoint:

- Registra la información HTTP saliente tal como es generada por `HttpClient`.
- No aplica filtrado de headers.
- No aplica límites de tamaño sobre el contenido registrado.
- No aplica masking automático de valores sensibles.

La responsabilidad de controlar qué información se registra se delega
completamente al consumidor de la librería, quien puede:

- evitar incluir información sensible en llamadas salientes
- implementar sanitización previa al envío
- o encapsular `HttpClient` con handlers personalizados

La incorporación de mecanismos de filtrado, límites de tamaño o masking
queda abierta para versiones futuras.


---

## Consecuencias

### Positivas
- Visibilidad clara de dependencias externas.
- Mejor diagnóstico de fallos fuera del sistema.
- Análisis de latencia y performance de integraciones.
- Reducción de incertidumbre en producción.

### Negativas / trade-offs
- Registro excesivo puede aumentar volumen de datos.
- Requiere configuración explícita por `HttpClient`.
- No captura llamadas realizadas fuera de `HttpClientFactory`.

---

## Extensibilidad

LogEndpoint puede ampliarse para:
- correlación con logs de entrada (traceId)
- integración con políticas de resiliencia
- métricas avanzadas por endpoint externo

No intenta reemplazar soluciones de observabilidad completas,
sino ofrecer trazabilidad básica y controlada.

---

## Implicaciones para otros módulos

- **LogHttp** captura errores entrantes.
- **LogAudit** audita requests normales entrantes.
- **LogEndpoint** cubre el flujo saliente.
- Los **Workers de limpieza** controlan la retención de estos registros.

---

## Notas finales

LogEndpoint completa el ciclo de observabilidad de ReecInspection:
- entrada (LogAudit)
- error (LogHttp)
- salida (LogEndpoint)

Cualquier cambio en este enfoque debe evaluarse mediante un nuevo ADR.
