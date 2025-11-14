using Microsoft.EntityFrameworkCore;
using Reec.Inspection.Extensions;
using Reec.Inspection.SqlServer;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddReecException<DbContextSqlServer>(options =>
//                options.UseSqlServer(configuration.GetConnectionString("default")),
//                new ReecExceptionOptions
//                {
//                    ApplicationName = "Reec.Inspecion.Api",
//                    EnableMigrations = false,
//                    EnableProblemDetails = true
//                });

builder.Services.AddReecInspection<DbContextSqlServer>(options =>
    options.UseSqlServer(configuration.GetConnectionString("default")),
    options =>
    {
        options.ApplicationName = "Reec.Inspecion.Api";
        options.EnableMigrations = true;
        options.EnableProblemDetails = true;
        options.SystemTimeZoneId = "SA Pacific Standard Time"; //valor por defecto.

        //Ejemplos de configuración de opciones de logs para limpieza periódica.
        options.LogAudit.CronValue = "1/1 * * * *"; // Cada 1 minuto
        options.LogEndpoint.CronValue = "0 */1 * * *"; // Cada 1 hora
        options.LogJob.CronValue = "0 */1 * * *"; 
        options.LogHttp.CronValue = "0 */1 * * *";
    });


var httpBuilder = builder.Services.AddHttpClient("PlaceHolder", httpClient =>
{
    httpClient.DefaultRequestHeaders.Clear();
    httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
});
builder.Services.AddReecInspectionResilience(httpBuilder);


var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseReecInspection();




app.UseAuthorization();

app.MapControllers();

app.Run();
