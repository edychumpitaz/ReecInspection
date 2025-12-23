# Matriz de dependencias (baseline probado)

Esta página resume las **dependencias y versiones recomendadas** (baseline probado) por **TFM** para los paquetes principales de **ReecInspection**.

> Nota: puedes usar versiones superiores según tu pipeline de seguridad/compatibilidad. Este baseline busca estabilidad y reproducibilidad.

---

## Reec.Inspection

| Dependencia | net8.0 | net9.0 | net10.0 | Comentario |
|---|---:|---:|---:|---|
| Cronos | 0.11.1 | 0.11.1 | 0.11.1 | Scheduler CRON usado por workers de limpieza |
| Microsoft.EntityFrameworkCore | 8.0.22 | 9.0.6 | 10.0.0 | ORM principal |
| Microsoft.EntityFrameworkCore.Relational | 8.0.22 | 9.0.6 | 10.0.0 | Soporte relacional EF Core |
| Microsoft.AspNetCore.MiddlewareAnalysis | 8.0.22 | 9.0.6 | 10.0.0 | Instrumentación de pipeline HTTP |
| Microsoft.Extensions.Http.Resilience | 8.10.0 | 9.6.0 | 10.0.0 | Resiliencia HTTP (Polly-based) |

---

## Reec.Inspection.SqlServer

| Dependencia | net8.0 | net9.0 | net10.0 | Comentario |
|---|---:|---:|---:|---|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.22 | 9.0.6 | 10.0.0 | Provider SQL Server |
| Microsoft.EntityFrameworkCore.Design | 8.0.22 | 9.0.6 | 10.0.0 | Migraciones (`PrivateAssets=all`) |
| Microsoft.EntityFrameworkCore.Tools | 8.0.22 | 9.0.6 | 10.0.0 | CLI / tooling (`PrivateAssets=all`) |

---

## Política de actualización de dependencias

- Cada dependencia se publica con un **baseline probado** por TFM.
- El baseline se revisa aproximadamente cada **6 meses**.
- Regla práctica: el baseline suele avanzar alrededor de **~6 versiones de patch** cuando corresponde.
- El consumidor puede actualizar a versiones superiores según sus políticas de seguridad y pipeline CI/CD.

---

## ¿Cómo usar esta matriz?

- Si tu app está en **net8.0**: usa la columna `net8.0` como baseline.
- Si multi-targeteas (**net8.0; net9.0; net10.0**): alinea cada TFM con su columna (evitas `MissingMethodException` por desalineo de EF Core).
- Si actualizas una dependencia “grande” (ej. EF Core): intenta mantener el resto del stack del mismo major por TFM (EF + Relational + Provider + Tooling).

