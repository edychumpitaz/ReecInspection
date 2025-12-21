# ADR-01 — Objetivo y alcance de Reec.Inspection (mini-APM pragmático)

## Estado
Aceptado

---

## Contexto

La mayoría de equipos necesita un nivel mínimo de observabilidad para operar en producción:

- detectar errores y su contexto
- auditar requests relevantes
- medir tiempos de ejecución
- rastrear integraciones HTTP salientes
- ejecutar y trazar jobs en background (limpiezas, sincronizaciones, etc.)

En la práctica, muchos entornos no tienen (o no desean introducir) desde el inicio:

- plataformas completas de observabilidad (Jaeger, Application Insights, CloudWatch, etc.)
- stacks de logs dedicados (Graylog, Seq, etc.)
- soluciones integradas de trazabilidad total (por ejemplo, Aspire)

Por costo, complejidad operativa o curva de aprendizaje, varios proyectos terminan
sin observabilidad consistente y dependen de implementaciones manuales y dispersas.

---

## Problema

- La captura de errores suele quedar en `try/catch` por endpoint o implementaciones ad-hoc.
- La auditoría y trazabilidad no son uniformes.
- La visibilidad de llamadas salientes y jobs en background es incompleta.
- Equipos pequeños necesitan algo usable desde el día 1, sin infraestructura adicional.

Se requiere un enfoque:
- simple
- controlado
- que se adopte rápido
- y que no obligue a herramientas externas para ser útil en producción.

---

## Decisión

Se define a **Reec.Inspection** como una librería de **observabilidad mínima indispensable**
orientada a aplicaciones .NET, con un enfoque de **mini-APM pragmático**.

El objetivo es entregar, desde el inicio del proyecto, capacidades base para operar:

- **LogHttp**: captura automática y consistente de errores del pipeline HTTP
- **LogAudit**: auditoría de requests/responses
- **LogEndpoint**: trazabilidad de llamadas HTTP salientes
- **LogJob / IWorker**: ejecución segura y trazable de jobs en background
- **Workers de limpieza**: retención y control de crecimiento

La persistencia por defecto se apoya en **base de datos**, como componente común
ya conocido por la mayoría de equipos, evitando dependencias operativas adicionales.

La base de datos se establece como el primer destino de observabilidad
por defecto; la integración con plataformas externas se considera una
evolución natural cuando el contexto operativo lo requiere.


---

## Alcance (qué incluye)

Reec.Inspection incluye:

- Registro persistente (configurable) de:
  - errores HTTP entrantes (LogHttp)
  - auditoría HTTP (LogAudit)
  - llamadas HTTP salientes (LogEndpoint)
  - ejecución de jobs (LogJob)
- Configuración por módulo y a nivel global para habilitar/deshabilitar persistencia.
- Retención y limpieza programada para controlar volumen y costo.
- Diseño modular para separar “Core” de “Providers” (por ejemplo SQL Server).

---

## No-alcance (qué NO pretende ser)

Reec.Inspection **no** pretende:

- Reemplazar plataformas completas de observabilidad o APM:
  - Jaeger, Application Insights, CloudWatch, etc.
- Reemplazar sistemas dedicados de logs/consulta:
  - Graylog, Seq, ELK, etc.
- Proveer “trazabilidad total distribuida” como solución principal.
- Ser un framework de infraestructura o un runtime observability stack.

En cambio, busca ser el nivel mínimo y útil para producción, con baja fricción.

---

## Principios de diseño

- **Simplicidad de adopción**: integrable por DI y middleware/handlers estándar.
- **Observabilidad mínima viable**: cubrir entrada, error, salida y background.
- **Control explícito**: configuración por módulo, límites, retención, y persistencia opcional.
- **Extensible**: permitir evolución hacia OpenTelemetry u observabilidad externa sin reescribir contratos.
- **Costo controlado**: limpieza por lotes, retención por días y opciones “light” para jobs.
- **Base de datos como núcleo de observabilidad**: Reec.Inspection prioriza
  herramientas conocidas por los equipos como primer nivel de operación
  en producción, habilitando integraciones avanzadas solo cuando aportan
  valor real.


---

## Consecuencias

### Positivas
- Observabilidad consistente desde el día 1 con herramientas conocidas (BD).
- Menor dependencia de disciplina individual (captura centralizada).
- Base sólida para crecer a observabilidad avanzada cuando el proyecto lo justifique.
- Mejor diagnóstico y soporte en producción sin infraestructura adicional.

### Negativas / trade-offs
- Persistir logs en BD puede generar volumen; requiere retención y limpieza.
- No ofrece visualización avanzada out-of-the-box como plataformas dedicadas.
- Algunas capacidades avanzadas (tracing distribuido completo) se dejan a integraciones externas.

---

## Relación con observabilidad externa (evolución)

Reec.Inspection se concibe como una base que puede:

- funcionar sola (persistencia en BD) para escenarios de bajo/medio costo operativo
- convivir o migrar a soluciones avanzadas (OpenTelemetry, Jaeger, AppInsights, CloudWatch, etc.)
  cuando el cliente requiera escalamiento o trazabilidad completa

La librería prioriza un “upgrade path” sin romper el modelo mental del equipo.

---

## Notas finales

Este ADR establece la visión: Reec.Inspection es un **mini-APM pragmático**
para operar en producción con fricción mínima, sin competir con suites completas.
Cualquier expansión de alcance debe evaluarse mediante nuevos ADRs.
