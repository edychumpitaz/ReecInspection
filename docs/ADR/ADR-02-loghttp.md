# ADR-02 — Captura automática de errores HTTP vía middleware (LogHttp)

## Estado
Aceptado

---

## Contexto

En aplicaciones API empresariales es frecuente que:

- Las excepciones no controladas se propaguen hasta el runtime.
- El manejo de errores dependa de `try/catch` por endpoint.
- El logging sea inconsistente entre equipos y servicios.
- Se pierda contexto crítico para diagnóstico (request, headers, traceId).

Además:

- El manejo manual de errores introduce código repetitivo.
- No todos los desarrolladores interceptan correctamente excepciones técnicas.
- La observabilidad suele depender de disciplina individual.

**ReecInspection** busca centralizar la captura de errores HTTP, garantizando
trazabilidad, persistencia controlada y una respuesta consistente al cliente,
sin exigir lógica adicional al desarrollador.

---

## Decisión

Se decide implementar **LogHttp** como un **middleware global de ASP.NET Core** que:

- Intercepte **todas las excepciones no controladas** del pipeline HTTP.
- Capture automáticamente:
  - excepción y stack trace
  - información del request (método, ruta, trace identifier)
  - headers configurados explícitamente
  - body del request cuando esté habilitado
- Persista esta información en la entidad **LogHttp**, de forma configurable.
- Devuelva una respuesta HTTP consistente al cliente sin exponer detalles internos.

La captura de errores se realiza **a nivel de middleware**, no mediante
`try/catch` manuales ni filtros por endpoint.

---

## Alternativas consideradas

### 1) Manejo de excepciones por endpoint (`try/catch`)
- ❌ Código repetitivo
- ❌ Dependiente de la disciplina del desarrollador
- ❌ Difícil de auditar y estandarizar

### 2) Exception Filters
- ⚠️ Limitados a MVC / Controllers
- ⚠️ Menor control del pipeline completo
- ⚠️ No cubren todos los escenarios (Minimal APIs, middlewares previos)

### 3) Middleware global (elegida)
- ✅ Captura centralizada
- ✅ Aplica a todo el pipeline HTTP
- ✅ Independiente del tipo de API
- ✅ Permite decisiones uniformes de logging y respuesta

---

## Detalles de implementación

- LogHttp se registra como middleware temprano en el pipeline.
- Diferencia entre:
  - **Excepciones controladas** (`ReecException`)
  - **Excepciones no controladas** (`Exception`)
- El formato de respuesta puede ser:
  - **Legacy** (`ReecMessage`)
  - **Estándar** (`ProblemDetails`, RFC 7807)
- La persistencia en base de datos es configurable mediante:
  - `EnableGlobalDbSave`
  - opciones específicas del módulo LogHttp
- La captura de body y headers:
  - tiene límites de tamaño configurables
  - utiliza buffering opcional, consciente del costo en memoria

---

## Consideraciones de seguridad y privacidad (PII)

- La librería **no decide automáticamente qué headers son sensibles**.
- La selección de headers a persistir se controla mediante:
  - `HeaderKeysInclude` (whitelist)
  - `HeaderKeysExclude` (blacklist)
- Si `HeaderKeysInclude` está configurado, tiene prioridad sobre `Exclude`.

**No se aplica masking automático de valores sensibles.**  
La responsabilidad de sanitización o exclusión de datos se delega al consumidor,
quien puede:
- configurar exclusiones
- o implementar un middleware previo para sanitización avanzada

---

## Consecuencias

### Positivas
- Eliminación de `try/catch` repetitivos en endpoints.
- Logging de errores consistente y estructurado.
- Mejor trazabilidad y diagnóstico en producción.
- Menor carga cognitiva para desarrolladores.

### Negativas / trade-offs
- El uso de buffering puede incrementar el consumo de memoria.
- Riesgo de capturar información sensible si se configura incorrectamente.
- Requiere un orden correcto en el pipeline de middlewares.

---

## Extensibilidad

LogHttp está diseñado para coexistir con middlewares superiores que gestionen:
- buffering avanzado
- sanitización personalizada
- estrategias de observabilidad específicas

El consumidor puede desactivar funcionalidades internas y delegar estas
responsabilidades sin romper el pipeline de ReecInspection.

---

## Implicaciones para otros módulos

- **LogAudit** complementa a LogHttp, pero no lo reemplaza.
- **LogEndpoint** captura llamadas HTTP salientes; LogHttp captura errores entrantes.
- **Workers de limpieza** son necesarios para controlar la retención de LogHttp.
- Las decisiones de persistencia y privacidad impactan directamente en este módulo.

---

## Notas finales

LogHttp representa la **decisión fundacional** de ReecInspection:
priorizar observabilidad, estabilidad y control centralizado
sobre manejo manual de errores por parte del desarrollador.

Cualquier cambio a este enfoque debe evaluarse mediante un nuevo ADR.
