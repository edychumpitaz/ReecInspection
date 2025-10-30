using Microsoft.EntityFrameworkCore;
using Reec.Inspection.Extensions;
using Reec.Inspection.Options;
using Reec.Inspection.SqlServer;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddReecException<DbContextSqlServer>(options =>
                options.UseSqlServer(configuration.GetConnectionString("default")),
                new ReecExceptionOptions
                {
                    ApplicationName = "Reec.Inspecion.Api",
                    EnableMigrations = true,
                    EnableProblemDetails = true
                });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseReecException<DbContextSqlServer>();

app.UseAuthorization();

app.MapControllers();

app.Run();
