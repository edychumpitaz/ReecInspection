using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reec.Inspection;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using Reec.Inspection.Workers;

namespace Reec.Test.xUnit.Helpers
{
    /// <summary>
    /// Factory para crear instancias de InspectionDbContext en memoria para testing
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Crea un DbContext SQLite en memoria con todos los servicios necesarios
        /// </summary>
        /// <param name="databaseName">Nombre único para la base de datos</param>
        /// <returns>Tupla con el contexto, opciones y el ServiceProvider (debe ser disposed)</returns>
        public static (TestInspectionDbContext Context, ReecExceptionOptions Options, IServiceProvider ServiceProvider) 
            CreateInMemoryContextWithServices(string databaseName = "TestDb")
        {
            var services = new ServiceCollection();
            var options = CreateDefaultOptions();
            
            // Registrar logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            
            // Registrar opciones
            services.AddSingleton(options);
            services.AddScoped<IDateTimeService, DateTimeService>();
            
            // SQLite In-Memory - la conexión debe mantenerse abierta
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            
            // Registrar DbContext con SQLite
            services.AddDbContext<TestInspectionDbContext>(opt =>
            {
                opt.UseSqlite(connection)
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors();
            });

            // Registrar como InspectionDbContext para compatibilidad
            services.AddScoped<InspectionDbContext>(sp => sp.GetRequiredService<TestInspectionDbContext>());
            
            // Registrar IDbContextService
            services.AddScoped<IDbContextService>(sp => 
                new DbContextService<TestInspectionDbContext>(
                    sp.GetRequiredService<TestInspectionDbContext>()));

            var serviceProvider = services.BuildServiceProvider();
            
            // Obtener el contexto y crear el esquema
            var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TestInspectionDbContext>();
            context.Database.EnsureCreated();

            return (context, options, serviceProvider);
        }

        /// <summary>
        /// Crea un ServiceProvider completo con IWorker para testing de workers
        /// </summary>
        public static (TestInspectionDbContext Context, ReecExceptionOptions Options, IServiceProvider ServiceProvider)
            CreateInMemoryContextWithWorker(string databaseName = "TestDb")
        {
            var services = new ServiceCollection();
            var options = CreateDefaultOptions();

            // Registrar logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // Registrar HttpContextAccessor (requerido por IWorker)
            services.AddHttpContextAccessor();

            // Registrar opciones
            services.AddSingleton(options);
            services.AddScoped<IDateTimeService, DateTimeService>();

            // SQLite In-Memory
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            // Registrar DbContext
            services.AddDbContext<TestInspectionDbContext>(opt =>
            {
                opt.UseSqlite(connection)
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors();
            });

            // Registrar como InspectionDbContext
            services.AddScoped<InspectionDbContext>(sp => sp.GetRequiredService<TestInspectionDbContext>());

            // Registrar IDbContextService
            services.AddScoped<IDbContextService>(sp =>
                new DbContextService<TestInspectionDbContext>(
                    sp.GetRequiredService<TestInspectionDbContext>()));

            // Registrar IWorker
            services.AddScoped<IWorker, Worker>();

            var serviceProvider = services.BuildServiceProvider();

            // Crear esquema
            var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TestInspectionDbContext>();
            context.Database.EnsureCreated();

            return (context, options, serviceProvider);
        }

        /// <summary>
        /// Crea un DbContext SQLite en memoria simple (para tests que no necesitan DI)
        /// </summary>
        public static TestInspectionDbContext CreateInMemoryContext(string databaseName = "TestDb")
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<TestInspectionDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options;

            var context = new TestInspectionDbContext(options);
            
            // Crear el esquema
            context.Database.EnsureCreated();

            return context;
        }

        /// <summary>
        /// Crea opciones por defecto para testing
        /// </summary>
        public static ReecExceptionOptions CreateDefaultOptions()
        {
            return new ReecExceptionOptions
            {
                ApplicationName = "Reec.Test",
                SystemTimeZoneId = "SA Pacific Standard Time",
                EnableMigrations = false,
                EnableProblemDetails = false,
                EnableGlobalDbSave = true,
                MinCategory = Reec.Inspection.ReecEnums.Category.Unauthorized,
                LogHttp = new LogHttpOption
                {
                    IsSaveDB = true,
                    EnableBuffering = true,
                    Schema = null,
                    TableName = "LogHttp"
                },
                LogAudit = new LogAuditOption
                {
                    IsSaveDB = true,
                    EnableBuffering = true,
                    ExcludePaths = new[] { "health", "swagger" },
                    Schema = null,
                    TableName = "LogAudit",
                    RequestBodyMaxSize = 10 * 1024,  // 10KB
                    ResponseBodyMaxSize = 10 * 1024  // 10KB
                },
                LogJob = new LogJobOption
                {
                    IsSaveDB = true,
                    Schema = null,
                    TableName = "LogJob"
                },
                LogEndpoint = new LogEndpointOption
                {
                    IsSaveDB = true,
                    Schema = null,
                    TableName = "LogEndpoint"
                }
            };
        }

        /// <summary>
        /// Limpia y elimina el contexto de forma segura
        /// </summary>
        public static void CleanupContext(InspectionDbContext context)
        {
            try
            {
                context?.Database.EnsureDeleted();
                context?.Dispose();
            }
            catch
            {
                // Ignorar errores al limpiar
            }
        }

        /// <summary>
        /// Limpia contexto y ServiceProvider
        /// </summary>
        public static void CleanupContextWithServices(InspectionDbContext context, IServiceProvider serviceProvider)
        {
            try
            {
                context?.Database.EnsureDeleted();
                context?.Dispose();
                (serviceProvider as IDisposable)?.Dispose();
            }
            catch
            {
                // Ignorar errores al limpiar
            }
        }
    }
}
