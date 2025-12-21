# ADR-08 — Providers de persistencia como paquetes separados (SqlServer y futuros)

## Estado
Aceptado

---

## Contexto

Reec.Inspection provee capacidades de observabilidad mínima mediante un
núcleo común (Core) que define:

- modelos de log
- contratos de ejecución
- reglas de persistencia y control
- decisiones de correlación y retención

Sin embargo, los mecanismos concretos de persistencia (por ejemplo SQL Server)
dependen de tecnologías específicas y librerías externas como ORMs, drivers
o SDKs.

Incluir estas dependencias directamente en el núcleo implicaría:

- acoplar Reec.Inspection a una tecnología concreta
- incrementar el peso y complejidad del paquete base
- dificultar la adopción en entornos con otras bases de datos
- bloquear la evolución hacia observabilidad externa

---

## Problema

- No todos los consumidores usan la misma base de datos.
- No todos desean persistir en base de datos.
- Algunos escenarios requieren múltiples destinos de observabilidad.
- El core no debe depender de detalles de infraestructura.

Se requiere un modelo que permita:
- mantener un núcleo liviano y estable
- agregar persistencia como extensión
- habilitar múltiples proveedores sin romper contratos

---

## Decisión

Se adopta un modelo **Core + Providers**, donde:

- **Reec.Inspection** actúa como núcleo:
  - define contratos, modelos y comportamiento base
  - no depende de tecnologías de persistencia concretas
- **Los providers de persistencia se distribuyen como paquetes NuGet separados**

El provider actual es:

- `Reec.Inspection.SqlServer`

Y el diseño permite incorporar en el futuro:

- `Reec.Inspection.PostgreSql`
- `Reec.Inspection.MySql`
- `Reec.Inspection.OpenTelemetry`
- `Reec.Inspection.ApplicationInsights`
- otros destinos según necesidad

---

## Alcance del Core

El núcleo **Reec.Inspection**: Actualmente, el core asume un mecanismo de persistencia basado en EF Core,
como decisión pragmática para facilitar la adopción inicial.


- Construye los objetos de observabilidad:
  - LogHttp
  - LogAudit
  - LogEndpoint
  - LogJob
- Aplica reglas de:
  - persistencia global y por módulo
  - correlación (TraceIdentifier / RequestId)
  - retención (conceptual)
- Expone contratos para que los providers puedan:
  - persistir
  - exportar
  - descartar información

En el estado actual, el núcleo Reec.Inspection ejecuta operaciones de
persistencia a través de Entity Framework Core (por ejemplo `SaveChanges`),
delegando la definición de la infraestructura concreta (DbContext,
migraciones y motor de base de datos) a los providers, como
`Reec.Inspection.SqlServer`.

La dirección arquitectónica del proyecto es evolucionar hacia un modelo
donde el core construya y emita eventos de observabilidad, y los providers
sean los únicos responsables de ejecutar operaciones de infraestructura
concreta.


---

## Alcance del Provider SqlServer

El paquete `Reec.Inspection.SqlServer`:

- Implementa los contratos definidos por el core.
- Contiene:
  - dependencias a Entity Framework Core
  - modelos de base de datos
  - migraciones
  - lógica de persistencia concreta
- Es responsable de:
  - mapear los logs a tablas físicas
  - ejecutar operaciones de guardado
  - aplicar optimizaciones específicas del motor

---

## Alternativas consideradas

### 1) Persistencia incluida en el core
- ❌ Alto acoplamiento
- ❌ Paquete base pesado
- ❌ Difícil evolución tecnológica

### 2) Provider único oficial (sin extensibilidad)
- ❌ Limita adopción
- ❌ No permite múltiples destinos
- ❌ Bloquea observabilidad externa

### 3) Core + Providers separados (elegida)
- ✅ Núcleo liviano y estable
- ✅ Infraestructura desacoplada
- ✅ Evolución sin romper contratos
- ✅ Alineado a patrones conocidos (`ILogger`)

---

## Relación con persistencia y evolución

Este modelo habilita:

- Persistencia directa en BD (fase inicial).
- Persistencia selectiva por módulo.
- Desactivación de BD y emisión a observabilidad externa.
- Convivencia de múltiples providers según entorno.

El proveedor activo se define por configuración y paquetes referenciados,
no por cambios en el código de negocio.

---

## Implicaciones para retención y limpieza

- Los workers de limpieza operan sobre el provider activo.
- Cada provider define cómo se ejecuta la eliminación (batching, optimización).
- El core mantiene la intención; el provider implementa la mecánica.

---

## Consecuencias

### Positivas
- Separación clara entre dominio y tecnología.
- Facilidad para agregar nuevos destinos de observabilidad.
- Menor fricción de adopción.
- Modelo entendible para desarrolladores .NET.

### Negativas / trade-offs
- Mayor número de paquetes a mantener.
- Requiere documentación clara de qué provider instalar.
- Cambios en contratos del core impactan a los providers.

---

## Notas finales

La separación Core + Providers es una decisión estructural
que define la escalabilidad y longevidad de Reec.Inspection.

Cualquier cambio que introduzca dependencias de infraestructura
en el núcleo debe evaluarse mediante un nuevo ADR.
