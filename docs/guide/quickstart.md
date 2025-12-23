# Quickstart

Esta guía muestra cómo integrar **Reec.Inspection** en una aplicación ASP.NET Core en pocos pasos.

---

## 1. Registrar servicios

En `Program.cs` registra Reec.Inspection indicando el `DbContext` y las opciones base:

```csharp
builder.Services.AddReecInspection<InspectionDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")),
    inspection =>
    {
        inspection.ApplicationName = "Reec.Inspection.Api";
        inspection.SystemTimeZoneId = "SA Pacific Standard Time";
        inspection.EnableProblemDetails = true;
    });
```


## 2. Registrar el middleware

Agrega el middleware de inspección al pipeline HTTP:

```csharp
var app = builder.Build();

app.UseReecInspection();

app.MapControllers();
app.Run();
```

## 3. Generar un error de prueba

Crea un endpoint que produzca un error intencional:

```csharp
[HttpGet("error")]
public IActionResult Error()
{
    var numerador = 1;
    var denominador = 0;
    var value = numerador / denominador;
    return Ok(value);
}
```

## 4. Resultado esperado
- El error es capturado automáticamente.
- Se registra un log HTTP en la base de datos.
- Si EnableProblemDetails = true, la respuesta sigue el estándar RFC 7807.
