# ADR-03 — Auditoría de requests y responses HTTP vía middleware (LogAudit)

## Estado
Aceptado

---

## Contexto

En aplicaciones empresariales no solo es importante capturar errores,
sino también **auditar el comportamiento normal del sistema**, por ejemplo:

- quién consume un endpoint
- qué ruta fue invocada
- cuánto tiempo tardó la operación
- qué status code fue retornado

Este tipo de información es necesaria para:
- auditoría funcional
- análisis de comportamiento
- cumplimiento normativo
- soporte y diagnóstico

Sin embargo:
- mezclar auditoría con manejo de errores genera ruido
- auditar todo sin control impacta memoria y almacenamiento
- no todos los requests deben ser auditados

---

## Decisión

Se decide implementar **LogAudit** como un **middleware independiente**
encargado exclusivamente de la **auditoría de requests y responses HTTP**,
separado del manejo de errores.

LogAudit:

- Registra requests que **completan su ejecución normalmente**
- No captura ni gestiona excepciones (responsabilidad de LogHttp)
- Permite auditar información HTTP sin interferir con el flujo del pipeline

La auditoría se realiza de forma **opcional y configurable**.

---

## Alternativas consideradas

### 1) Unificar auditoría y errores en un solo middleware
- ❌ Responsabilidades mezcladas
- ❌ Mayor complejidad de configuración
- ❌ Dificultad para desactivar auditoría sin afectar errores

### 2) Auditoría manual por endpoint
- ❌ Código repetitivo
- ❌ Dependiente del desarrollador
- ❌ Difícil de mantener consistente

### 3) Middleware dedicado para auditoría (elegida)
- ✅ Separación clara de responsabilidades
- ✅ Activación y configuración independiente
- ✅ Menor impacto cognitivo para el desarrollador

---

## Detalles de implementación

- LogAudit se ejecuta como middleware en el pipeline HTTP.
- Registra únicamente requests que:
  - no fueron excluidos por configuración
  - completaron su ejecución (con status code)
- La información capturada incluye:
  - método HTTP
  - ruta
  - status code
  - duración de la petición
  - headers configurados explícitamente
  - body request/response cuando está habilitado
- Soporta exclusión de rutas mediante:
  - `ExcludePaths`
- El guardado en base de datos es configurable mediante:
  - `EnableGlobalDbSave`
  - opciones específicas del módulo LogAudit

---

## Consideraciones de seguridad y privacidad (PII)

Actualmente, LogAudit:

- No aplica filtrado fino de headers mediante listas de inclusión o exclusión.
- No aplica masking automático de valores sensibles.
- La captura de body está habilitada por defecto.

La responsabilidad de controlar qué información se audita se delega
al consumidor de la librería mediante:
- configuración del middleware
- exclusión de rutas
- o middlewares previos de sanitización

La incorporación de mecanismos de inclusión/exclusión de headers y
controles más finos de auditoría queda abierta para versiones futuras.


## Consecuencias

### Positivas
- Auditoría clara y separada del manejo de errores.
- Mejor visibilidad del uso normal de la API.
- Menor ruido en logs de error.
- Configuración flexible por entorno.

### Negativas / trade-offs
- Auditoría excesiva puede generar alto volumen de datos.
- Uso de buffering puede impactar memoria si se habilita sin control.
- Requiere una correcta configuración de exclusiones.

---

## Extensibilidad

LogAudit puede:
- desactivarse completamente
- convivir con middlewares superiores de auditoría avanzada
- delegar buffering o sanitización a implementaciones externas

No intenta reemplazar soluciones especializadas de auditoría,
sino proveer una base consistente y liviana.

---

## Implicaciones para otros módulos

- **LogHttp** gestiona errores y excepciones, no auditoría.
- **LogEndpoint** cubre auditoría de llamadas salientes.
- **Workers de limpieza** controlan la retención de registros LogAudit.
- Las decisiones de privacidad y persistencia impactan directamente este módulo.

---

## Notas finales

LogAudit refuerza el principio de **separación de responsabilidades** en
ReecInspection:  
los errores se tratan como errores, y la auditoría como auditoría.

Cualquier cambio que mezcle estos conceptos debe evaluarse mediante un nuevo ADR.
