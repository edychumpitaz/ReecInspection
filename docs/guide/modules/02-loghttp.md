# LogHttp (Errores)

`LogHttp` es el módulo de Reec.Inspection orientado a **capturar errores HTTP**
y la evidencia necesaria para diagnosticarlos.

Su objetivo es responder preguntas como:

- ¿Qué request provocó el error?
- ¿Qué headers e IP tenía el cliente?
- ¿Qué excepción ocurrió realmente?
- ¿A qué aplicación pertenece este error?

> Este documento se enfoca en **cómo usar LogHttp**, asumiendo que la configuración base
> (provider, `ApplicationName`, middleware) ya fue realizada.

---

## 1. ¿Qué registra LogHttp?

Cuando ocurre un error HTTP, `LogHttp` registra:

- Request: método, ruta, query, headers (según configuración), body (si aplica).
- Response: status code y body (si aplica).
- Excepción capturada.
- Metadatos:
  - `ApplicationName`
  - `TraceIdentifier`
  - `RequestId`
  - Duración y timestamps.

---

## 2. Configuración por módulo

`LogHttp` se configura mediante `LogHttpOption`.

### 2.1 Parámetros comunes (compartidos por módulos)

Estos campos existen en **todos los módulos** y son usados también por
workers y procesos de limpieza.

| Propiedad | Descripción | Default |
|---|---|---|
| `IsSaveDB` | Habilita/deshabilita persistencia en base de datos. | `true` |
| `Schema` | Esquema donde se almacenará la tabla. | `null` |
| `TableName` | Nombre de la tabla del módulo. | `"LogHttp"` |
| `EnableClean` | Habilita limpieza automática por retención. | `true` |
| `CronValue` | Expresión CRON para ejecutar la limpieza. | `"0 2 * * *"` |
| `DeleteDays` | Retención en días. | `10` |
| `DeleteBatch` | Tamaño de borrado por lote. | `100` |

> La limpieza filtra por `CreateDateOnly` y puede acotar por `ApplicationName`.
> Para buen performance se recomienda indexar:
> - `CreateDateOnly`
> - `(ApplicationName, CreateDateOnly)` en escenarios multi-app.

---

### 2.2 Parámetros específicos de LogHttp

| Propiedad | Descripción | Default |
|---|---|---|
| `HeaderKeysInclude` | Lista explícita de headers permitidos para persistencia. **Tiene prioridad** sobre `HeaderKeysExclude`. | `null` |
| `HeaderKeysExclude` | Lista de headers que no deben persistirse si no se usa include-list. | `null` |
| `IpAddressFromHeader` | Header usado para obtener la IP real del cliente. | `null` |
| `RequestIdFromHeader` | Header usado como identificador de correlación. | `null` |
| `EnableBuffering` | Habilita buffering del request para poder leer el body sin romper el pipeline. | `true` |

---

## 3. Control de headers (Include / Exclude)

### 3.1 Orden de validación

LogHttp aplica las reglas en este orden:

1. **`HeaderKeysInclude`**
   - Si tiene valores, **solo esos headers** se persisten.
2. **`HeaderKeysExclude`**
   - Se evalúa únicamente si `HeaderKeysInclude` es `null`.

---

### 3.2 Ejemplo: excluir headers sensibles

```csharp
options.LogHttp.HeaderKeysExclude = new List<string>
{
    "Authorization",
    "Cookie",
    "Set-Cookie",
    "X-Api-Key"
};
```

---

### 3.3 Ejemplo: allow-list estricta

```csharp
options.LogHttp.HeaderKeysInclude = new List<string>
{
    "User-Agent",
    "X-Request-Id",
    "X-Forwarded-For"
};
```

---

## 4. IP real y correlación de requests

### 4.1 IP desde proxy / gateway

```csharp
options.LogHttp.IpAddressFromHeader = "X-Forwarded-For";
```

### 4.2 RequestId desde header

```csharp
options.LogHttp.RequestIdFromHeader = "X-Request-Id";
```

---

## 5. EnableBuffering

`EnableBuffering` viene **activado por defecto**.

Es necesario para poder leer el body del request sin romper el pipeline HTTP.

---

## 6. Limpieza automática (retención)

```csharp
options.LogHttp.EnableClean = true;
options.LogHttp.CronValue = "0 3 * * *";
options.LogHttp.DeleteDays = 15;
options.LogHttp.DeleteBatch = 500;
```

---

## 7. Checklist rápido

- `ApplicationName` definido.
- Headers sensibles controlados.
- IP y RequestId configurados.
- Retención activa.
- Índices por `CreateDateOnly`.
