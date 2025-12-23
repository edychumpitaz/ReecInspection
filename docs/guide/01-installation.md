## Requisitos

- .NET 8+  
  TFMs soportados:
  - net8.0
  - net9.0
  - net10.0

- Entity Framework Core compatible con el TFM utilizado.

- Un provider de base de datos relacional.
  - SQL Server disponible mediante `Reec.Inspection.SqlServer`.

> Recomendación: definir un esquema dedicado (por ejemplo `Inspection`) para aislar las tablas de logs y auditoría.
