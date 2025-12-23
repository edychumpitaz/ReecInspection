# Providers

Los **Providers** en Reec.Inspection definen **cómo y dónde se persiste la información de inspección**.
Permiten desacoplar la lógica de captura (LogHttp, LogAudit, LogEndpoint, LogJob) del motor de almacenamiento.

Actualmente el proveedor principal es **SQL Server**, pero el diseño permite extender a otros motores.

---

## 1. ¿Qué es un Provider?

Un **Provider** es el responsable de:

- Persistir logs en la base de datos.
- Ejecutar limpiezas automáticas (retención).
- Resolver esquemas y tablas por módulo.
- Centralizar decisiones de almacenamiento.

Cada módulo (LogHttp, LogAudit, etc.) delega al provider la operación de guardado.

---

## 2. Provider SQL Server

El provider **SqlServer** se incluye mediante el paquete:

```bash
dotnet add package Reec.Inspection.SqlServer
```

### 2.1 Registro básico

```csharp
builder.Services.AddReecInspection(
    options =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"));
    });
```

Este registro habilita:

- Persistencia de logs.
- Migraciones automáticas (si están habilitadas).
- Limpieza por CRON si está configurada.

---

## 3. Configuración por módulo

Cada módulo puede definir:

| Propiedad | Descripción |
|----------|------------|
| IsSaveDB | Habilita/deshabilita persistencia |
| Schema | Esquema de BD |
| TableName | Nombre de tabla |
| EnableClean | Activa limpieza automática |
| CronValue | CRON de ejecución |
| DeleteDays | Días de retención |
| DeleteBatch | Tamaño de borrado por lote |

### Ejemplo completo

```csharp
builder.Services.AddReecInspection(
    db => db.UseSqlServer(connectionString),
    inspection =>
    {
        inspection.LogHttp.IsSaveDB = true;
        inspection.LogHttp.Schema = "Inspection";
        inspection.LogHttp.TableName = "LogHttp";
        inspection.LogHttp.EnableClean = true;
        inspection.LogHttp.CronValue = "0 2 * * *";
        inspection.LogHttp.DeleteDays = 15;
        inspection.LogHttp.DeleteBatch = 500;
    });
```

---

## 4. Migraciones automáticas

Por defecto, el provider puede ejecutar migraciones al iniciar:

```csharp
inspection.EnableMigrations = true;
```

### Recomendaciones

- **DEV / QA**: habilitar migraciones automáticas.
- **PRD**: deshabilitar y aplicar scripts controlados.

---

## 5. Buenas prácticas

- Usa un **schema dedicado** (ej. `Inspection`).
- Indexa columnas de fecha para limpieza eficiente.
- Evita tablas compartidas con dominios funcionales.
- Escalona horarios de limpieza entre módulos.

---

## 6. Extensibilidad futura

El diseño permite agregar nuevos providers:

- PostgreSQL
- Oracle
- File / Blob Storage
- Providers híbridos (DB + Queue)

Cada provider implementa el mismo contrato, manteniendo intactos los módulos de inspección.
