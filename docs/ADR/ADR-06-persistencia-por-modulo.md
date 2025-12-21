# ADR-06 — Persistencia por módulo + EnableGlobalDbSave (control de costo y adopción)

## Estado
Aceptado

---

## Contexto

Reec.Inspection provee observabilidad mínima mediante módulos (LogHttp, LogAudit, LogEndpoint, LogJob).
Estos módulos pueden generar un volumen significativo de registros en producción.

En muchos equipos, la base de datos es la herramienta más disponible y conocida para operar (mínimo viable),
pero no siempre se desea:

- persistir todo (por volumen/costo)
- persistir en todos los entornos (DEV/QA/PRD)
- acoplarse a BD cuando se quiere enviar a observabilidad externa (OpenTelemetry / AppInsights / CloudWatch)

Se requiere una estrategia que permita:
- habilitar/deshabilitar persistencia de forma centralizada
- activar/desactivar por módulo sin reescribir lógica
- controlar costo (storage/IO)
- mantener adopción simple (plug & play)

---

## Problema

Sin un control uniforme de persistencia:

- cada módulo decide por su cuenta cuándo guarda (inconsistente)
- el consumidor debe modificar código para “apagar” logs (alto costo de adopción)
- se corre el riesgo de sobre-registrar (BD crece sin control)
- la migración a observabilidad externa es más difícil porque “todo asume BD”

---

## Decisión

Se adopta un esquema de persistencia con doble control:

1) **Control global**: `EnableGlobalDbSave`
   - Define si Reec.Inspection tiene permitido persistir en BD en el proceso actual.
   - Permite apagar toda la persistencia (por ejemplo en DEV, o al migrar a observabilidad externa).

2) **Control por módulo** (options específicas: LogHttpOption, LogAuditOption, LogEndpointOption, LogJobOption, etc.)
   - Permite habilitar/deshabilitar guardado por cada módulo.
   - Permite mantener solo lo indispensable según el contexto del cliente.

La lógica de decisión de guardado sigue este principio:
- Si `EnableGlobalDbSave` es `false` → **no se persiste nada**.
- Si `EnableGlobalDbSave` es `true` → cada módulo decide según su option/flag.

---

## Alternativas consideradas

### 1) Persistir siempre (sin switches)
- ❌ Alto riesgo de volumen y costo
- ❌ Difícil de adoptar en clientes con restricciones
- ❌ Complica la migración a observabilidad externa

### 2) Switch único global (sin control por módulo)
- ❌ No permite “mínimo viable” por componente
- ❌ O es todo o nada; poca flexibilidad

### 3) Control global + control por módulo (elegida)
- ✅ Control de costo y volumen
- ✅ Adopción simple (configuración, no código)
- ✅ Permite estrategia “mínimo indispensable”
- ✅ Facilita migración gradual a herramientas externas

---

## Detalles de implementación

- `EnableGlobalDbSave` vive en opciones base (por ejemplo `ReecExceptionOptions` u objeto raíz de configuración).
- Cada módulo tiene su propia option con un flag de persistencia (por ejemplo `IsSave` / `IsSaveDb` o equivalente).
- La decisión de persistir se evalúa en el punto donde se construye el log (middleware/handler/worker), evitando
  que el consumidor tenga que implementar condiciones manuales.

---

## Consecuencias

### Positivas
- Control explícito de persistencia por entorno y por módulo.
- Reducción de volumen (se puede habilitar solo LogHttp en PRD, por ejemplo).
- Permite operar “solo con BD” desde el día 1.
- Camino limpio para: “BD off → envío a OpenTelemetry” sin reescribir módulos.

### Negativas / trade-offs
- Mayor cantidad de opciones/configuración a documentar.
- Si el consumidor desactiva demasiado, puede perder visibilidad (riesgo operativo).
- Requiere disciplina para definir defaults y recomendaciones por entorno.

---

## Implicaciones para otros módulos

- **ADR-07 (Retención y limpieza)**: si la persistencia está activa, retención es obligatoria para controlar costo.
- **ADR-30 (CorrelationContext)**: cuando la persistencia está apagada, el contexto/correlación sigue siendo útil
  para trazabilidad externa.
- **Providers (SQL Server, etc.)**: la decisión de persistencia impacta si se requiere o no el provider.

---

## Relación con observabilidad externa (evolución)

Este ADR habilita una estrategia escalable:

- Fase 1: Persistencia mínima en BD (sin herramientas externas).
- Fase 2: Persistencia selectiva por módulo.
- Fase 3: Desactivar la persistencia directa en base de datos y emitir
  eventos de observabilidad a un pipeline interno con proveedores (sinks)
  configurables, manteniendo el mismo modelo conceptual de logs.

  En este escenario, Reec.Inspection actúa como productor de telemetría
  (LogHttp, LogAudit, LogEndpoint, LogJob), delegando el destino final
  a implementaciones específicas (por ejemplo SQL Server, OpenTelemetry,
  Application Insights u otros).

  Este enfoque sigue un patrón similar a `ILogger`, donde el núcleo define
  el contrato y los proveedores se encargan de exportar la información,
  permitiendo desacoplar la librería de tecnologías de persistencia
  concretas y facilitar la evolución hacia observabilidad externa.


La base de datos es el primer destino de observabilidad por defecto; la integración con plataformas externas
es una evolución natural cuando el contexto operativo lo requiere.

---

## Notas finales

Esta decisión prioriza adopción y operación simple, sin bloquear el crecimiento a observabilidad avanzada.
Cualquier cambio a la jerarquía (global vs módulo) debe evaluarse mediante un nuevo ADR.
