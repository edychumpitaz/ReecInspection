# Roadmap — ReecInspection

Este roadmap describe la **evolución planificada** de Reec.Inspection.
No es una promesa de fechas, sino una **guía de dirección técnica** basada en estabilidad,
uso real y necesidades de observabilidad en sistemas .NET.

---

## Principios del roadmap

- Priorizar **estabilidad y compatibilidad** sobre nuevas features.
- Mantener una **API predecible**.
- Evolucionar por módulos, sin romper contratos existentes.
- Separar claramente:
  - Documentación de uso (Guías)
  - Decisiones técnicas (ADR)

---

## Corto plazo (consolidación)

Enfoque: robustecer lo existente y cerrar brechas de documentación.

### Documentación
- Completar documentación de módulos:
  - LogAudit
  - LogHttp
  - LogEndpoint
  - LogJob
- Documentar:
  - Esquemas de base de datos por módulo
  - Índices recomendados
  - Estrategias de limpieza y retención

### Estabilidad
- Alinear baselines de dependencias por TFM.
- Mejorar validaciones de configuración en arranque.
- Endurecer manejo de excepciones internas.

---

## Mediano plazo (capacidades)

Enfoque: ampliar capacidades sin acoplar el core.

### Observabilidad
- Mejorar correlación entre:
  - HTTP entrante
  - Jobs / Workers
  - Integraciones externas
- Exponer metadatos consistentes (`TraceIdentifier`, `ApplicationName`).

### Providers
- Mejoras incrementales al provider SQL Server.
- Preparar abstracciones para futuros providers sin romper contratos.

### Performance
- Optimización de procesos de limpieza batch.
- Revisión de impacto de buffering en escenarios de alta carga.

---

## Largo plazo (visión)

Enfoque: madurez del ecosistema.

### Arquitectura
- Posible separación de:
  - Core (contratos y modelos)
  - Providers
  - Integraciones opcionales
- Evaluar extensiones específicas por dominio.

### Integraciones
- Mejorar soporte a escenarios legacy.
- Facilitar adopción en arquitecturas híbridas (On-Premise / Cloud).

---

## Fuera de alcance (por ahora)

Para mantener el foco, **no forman parte del roadmap inmediato**:

- Dashboards UI.
- Visualización gráfica de logs.
- Reemplazar herramientas APM completas.
- Automatización avanzada con IA.

---

## Nota final

Reec.Inspection evoluciona con base en **uso real**, feedback y estabilidad.
Las decisiones técnicas relevantes se documentan mediante **ADR**,
garantizando trazabilidad y coherencia en el tiempo.
