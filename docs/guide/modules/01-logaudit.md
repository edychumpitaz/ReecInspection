# LogAudit

`LogAudit` es el módulo de **auditoría funcional** de Reec.Inspection: Registra **qué entró** (request) y **qué devolvió** tu sistema (response),
para tener evidencia y trazabilidad de operaciones importantes.

Este módulo está pensado para responder preguntas como:

- ¿Qué payload llegó a mi API y qué respondió el sistema?
- ¿Cuándo ocurrió y cuánto demoró?
- ¿Qué usuario/sistema originó la operación?
- ¿Cómo correlaciono auditoría con errores (`LogHttp`) y jobs (`LogJob`)?

> Si tu objetivo es capturar **errores** y su detalle técnico, usa `LogHttp`.  
> Si tu objetivo es auditar **operaciones** (request + response) usa `LogAudit`.

---

## 1. ¿Qué registra LogAudit?

A nivel conceptual, `LogAudit` captura (según tu implementación interna):

- Request: método, ruta, query, headers (si aplica), body (con límites).
- Response: status code, body (con límites).
- Metadatos: timestamps, duración, identificadores de trazabilidad/correlación.
- Resultado: éxito/fallo de la operación auditada.

> En próximas iteraciones documentaremos con capturas reales de la tabla en BD (SQL Server) como ejemplo visual.

---

## 2. Configuración por módulo

`LogAudit` se configura a través de `LogAuditOption`.

### 2.1 Parámetros comunes (compartidos por módulos)

| Propiedad | Descripción | Default |
|---|---|---|
| `IsSaveDB` | Habilita/deshabilita persistencia en base de datos. | `true` |
| `Schema` | Esquema donde se almacenarán las tablas del módulo. | `null` |
| `TableName` | Nombre de tabla del módulo. | `"LogAudit"` |
| `EnableClean` | Habilita limpieza automática por retención. | `true` |
| `CronValue` | Expresión CRON para ejecutar la limpieza. | `"0 2 * * *"` |
| `DeleteDays` | Retención en días (borrar lo más antiguo). | `10` |
| `DeleteBatch` | Tamaño de borrado por lote (evita locks largos). | `100` |

### 2.2 Parámetros específicos de LogAudit

| Propiedad | Descripción | Default |
|---|---|---|
| `RequestBodyMaxSize` | Tamaño máximo del **body de request** a guardar (bytes). | `32 * 1024` (32KB) |
| `ResponseBodyMaxSize` | Tamaño máximo del **body de response** a guardar (bytes). | `32 * 1024` (32KB) |
| `ExcludePaths` | Lista de segmentos/rutas a excluir de auditoría (por performance/ruido). | `["swagger","index","favicon"]` |
| `EnableBuffering` | Habilita buffering del request para poder leer el body sin romper el pipeline. | `true` |

---

## 3. Ejemplo de configuración (Program.cs)

Ejemplo recomendado (schema dedicado y ajustes de retención):

```csharp
builder.Services.AddReecInspection(options =>
{
    // Recomendación: schema dedicado
    options.LogAudit.Schema = "Inspection";
    options.LogAudit.TableName = "LogAudit";

    // Auditoría: límites de tamaño (evita payloads gigantes)
    options.LogAudit.RequestBodyMaxSize = 32 * 1024;
    options.LogAudit.ResponseBodyMaxSize = 32 * 1024;

    // Evita ruido (swagger, health, etc.)
    options.LogAudit.ExcludePaths = new List<string>
    {
        "swagger",
        "health",
        "favicon"
    };

    // Limpieza automática
    options.LogAudit.EnableClean = true;
    options.LogAudit.CronValue = "0 2 * * *"; // 2:00 a.m.
    options.LogAudit.DeleteDays = 10;
    options.LogAudit.DeleteBatch = 200;
});
```

> Ajusta `DeleteBatch` según el tamaño de tu tabla y capacidad de tu BD.  
> Si tienes alta volumetría, usa 200–1000 y asegúrate de tener índices por fecha `CreateDateOnly`.

---

## 4. Excluir rutas de auditoría (`ExcludePaths`)

`ExcludePaths` reduce “ruido” y consumo de BD.

Recomendaciones típicas:

- `swagger`
- `health`
- `metrics`
- `favicon`
- `index`

> Si quieres excluir una ruta sensible completa (ej. `/auth/token`), agrega el segmento correspondiente y valida que coincida con tu matching interno.

---

## 5. Body sizes y buffering

### 5.1 ¿Por qué limitar `RequestBodyMaxSize` / `ResponseBodyMaxSize`?

Porque auditoría sin límites puede:

- Disparar costos de almacenamiento.
- Guardar PII accidental (documentos, tokens, etc.).
- Afectar performance en endpoints de payload grande.

**Regla práctica:**
- Auditoría funcional: 4KB–32KB suele ser suficiente.
- Operaciones especiales: habilita “más tamaño” solo para rutas críticas.

### 5.2 `EnableBuffering`

Si `EnableBuffering = true`, el módulo puede leer el body del request sin romper el pipeline.
Esto es útil, pero tiene costo en memoria/IO dependiendo de payloads.

Recomendación:
- Mantener `true` en auditoría si vas a guardar request body.
- Si tu API suele recibir archivos grandes (multipart), excluye esa ruta o desactiva auditoría allí.

---

## 6. Limpieza automática (retención)

`LogAudit` soporta limpieza automática usando:

- `EnableClean`
- `CronValue`
- `DeleteDays`
- `DeleteBatch`

Ejemplo:

```csharp
options.LogAudit.EnableClean = true;
options.LogAudit.CronValue = "0 2 * * *"; // diario 2:00 a.m.
options.LogAudit.DeleteDays = 30;          // retención 30 días
options.LogAudit.DeleteBatch = 500;        // borrado por lotes
```

Buenas prácticas:

- Escalona horarios por módulo (2:00, 3:00, 4:00 a.m.) para no “picar” BD al mismo tiempo.
- Asegura índice por fecha de creación(`CreateDateOnly`) para acelerar borrado.

---

## 7. Seguridad y privacidad

Antes de habilitar auditoría en producción:

- No guardes tokens, passwords, secretos.
- En endpoints sensibles, considera:
  - Excluir ruta (`ExcludePaths`)
  - Reducir body max size
  - Sanitizar payloads (si tu librería lo soporta por configuración)

> En `LogHttp` ya trabajaremos explícitamente con include/exclude de headers y masking.  
> Para `LogAudit`, aplica el mismo principio: **auditabilidad sin exponer secretos**.

---

## 8. Checklist rápido

- [ ] ¿Definiste `Schema` dedicado (`Inspection`)?
- [ ] ¿Tienes límites de body (`32KB` u otro)?
- [ ] ¿Excluiste rutas ruidosas (swagger/health)?
- [ ] ¿Configuraste retención (`DeleteDays`) acorde al negocio?
- [ ] ¿Validaste que no se persistan secretos (tokens/passwords)?
