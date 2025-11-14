using Microsoft.EntityFrameworkCore;
using Reec.Inspection;
using Reec.Inspection.Entities;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Reec.Test.xUnit.Helpers
{
    /// <summary>
    /// DbContext de prueba que hereda de InspectionDbContext
    /// Optimizado para SQLite In-Memory con configuración simplificada
    /// </summary>
    public class TestInspectionDbContext : InspectionDbContext
    {
        public TestInspectionDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var serializationOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Configuración para LogHttp
            modelBuilder.Entity<LogHttp>(entity =>
            {
                entity.ToTable("LogHttp");
                entity.HasKey(e => e.IdLogHttp);
                entity.Property(e => e.IdLogHttp).ValueGeneratedOnAdd();
                entity.Property(e => e.ApplicationName).HasMaxLength(100);
                entity.Property(e => e.CategoryDescription).HasMaxLength(50);
                entity.Property(e => e.RequestId).HasMaxLength(100);
                entity.Property(e => e.Protocol).HasMaxLength(50);
                entity.Property(e => e.Method).HasMaxLength(100);
                entity.Property(e => e.Scheme).HasMaxLength(30);
                entity.Property(e => e.Host).HasMaxLength(150);
                entity.Property(e => e.HostPort).HasMaxLength(200);
                entity.Property(e => e.QueryString).HasMaxLength(2500);
                entity.Property(e => e.Source).HasMaxLength(200);
                entity.Property(e => e.TraceIdentifier).HasMaxLength(100);
                entity.Property(e => e.ContentType).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(30);
                entity.Property(e => e.CreateUser).HasMaxLength(40);

                // Configurar Dictionary como JSON
                entity.Property(e => e.RequestHeader)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions) ?? new Dictionary<string, string>());
            });

            // Configuración para LogAudit
            modelBuilder.Entity<LogAudit>(entity =>
            {
                entity.ToTable("LogAudit");
                entity.HasKey(e => e.IdLogAudit);
                entity.Property(e => e.IdLogAudit).ValueGeneratedOnAdd();
                entity.Property(e => e.ApplicationName).HasMaxLength(100);
                entity.Property(e => e.RequestId).HasMaxLength(100);
                entity.Property(e => e.Protocol).HasMaxLength(50);
                entity.Property(e => e.Method).HasMaxLength(100);
                entity.Property(e => e.Scheme).HasMaxLength(50);
                entity.Property(e => e.Host).HasMaxLength(150);
                entity.Property(e => e.HostPort).HasMaxLength(200);
                entity.Property(e => e.QueryString).HasMaxLength(2500);
                entity.Property(e => e.TraceIdentifier).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.CreateUser).HasMaxLength(40);

                // Configurar Dictionary como JSON
                entity.Property(e => e.RequestHeader)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions) ?? new Dictionary<string, string>());

                entity.Property(e => e.ResponseHeader)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions) ?? new Dictionary<string, string>());
            });

            // Configuración para LogEndpoint
            modelBuilder.Entity<LogEndpoint>(entity =>
            {
                entity.ToTable("LogEndpoint");
                entity.HasKey(e => e.IdLogEndpoint);
                entity.Property(e => e.IdLogEndpoint).ValueGeneratedOnAdd();
                entity.Property(e => e.ApplicationName).HasMaxLength(100);
                entity.Property(e => e.TraceIdentifier).HasMaxLength(100);
                entity.Property(e => e.Method).HasMaxLength(20);
                entity.Property(e => e.Schema).HasMaxLength(20);
                entity.Property(e => e.Host).HasMaxLength(150);
                entity.Property(e => e.HostPort).HasMaxLength(200);
                entity.Property(e => e.Path).HasMaxLength(800);
                entity.Property(e => e.QueryString).HasMaxLength(1000);
                entity.Property(e => e.CreateUser).HasMaxLength(50);

                // Configurar Dictionary como JSON
                entity.Property(e => e.RequestHeader)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions) ?? new Dictionary<string, string>());

                entity.Property(e => e.ResponseHeader)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions) ?? new Dictionary<string, string>());
            });

            // Configuración para LogJob
            modelBuilder.Entity<LogJob>(entity =>
            {
                entity.ToTable("LogJob");
                entity.HasKey(e => e.IdLogJob);
                entity.Property(e => e.IdLogJob).ValueGeneratedOnAdd();
                entity.Property(e => e.ApplicationName).HasMaxLength(100);
                entity.Property(e => e.NameJob).HasMaxLength(200).IsRequired();
                entity.Property(e => e.TraceIdentifier).HasMaxLength(100);
                entity.Property(e => e.Message).HasMaxLength(1000);
                entity.Property(e => e.CreateUser).HasMaxLength(50);

                // Configurar Dictionary<string, object> como JSON
                entity.Property(e => e.Data)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, serializationOptions) ?? new Dictionary<string, object>());
            });

            // No llamar a base.OnModelCreating() para evitar configuraciones de SQL Server
        }
    }
}
