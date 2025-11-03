using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Reec.Inspection.Entities;
using Reec.Inspection.Options;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection.SqlServer
{
    public class DbContextSqlServer : InspectionDbContext
    {
        private readonly ReecExceptionOptions _reecOptions;
        public DbContextSqlServer([NotNull] DbContextOptions<DbContextSqlServer> options,
            ReecExceptionOptions reecOptions) : base(options)
        {
            this._reecOptions = reecOptions;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("Ingreso a OnModelCreating:");

            var serializationOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var comparer = new ValueComparer<Dictionary<string, string>>(
                              (c1, c2) => c1.SequenceEqual(c2),
                               c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                               c => c);

            var comparerObject = new ValueComparer<Dictionary<string, object>>(
                              (c1, c2) => c1.SequenceEqual(c2),
                               c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                               c => c);

            modelBuilder.Entity<LogHttp>(entity =>
            {

                if (_reecOptions == null)
                    entity.ToTable("LogHttp");
                else if (!string.IsNullOrWhiteSpace(_reecOptions.LogHttp.Schema) &&
                         !string.IsNullOrWhiteSpace(_reecOptions.LogHttp.TableName))
                    entity.ToTable(_reecOptions.LogHttp.TableName, _reecOptions.LogHttp.Schema);
                else
                    entity.ToTable(_reecOptions.LogHttp.TableName);

                entity.HasKey(e => e.IdLogHttp);
                entity.Property(e => e.IdLogHttp).UseIdentityColumn();
                entity.Property(e => e.ApplicationName).HasColumnType("varchar(100)");
                entity.Property(e => e.CategoryDescription).HasMaxLength(50).HasColumnType("varchar(50)");
                entity.Property(e => e.MessageUser).HasColumnType("varchar(max)");
                entity.Property(e => e.Duration).HasColumnType("time(7)");
                entity.Property(e => e.RequestId).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.Property(e => e.ExceptionMessage).HasColumnType("varchar(max)");
                entity.Property(e => e.InnerExceptionMessage).HasColumnType("varchar(max)");
                entity.Property(e => e.Protocol).HasColumnType("varchar(50)");
                entity.Property(e => e.Method).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.Property(e => e.Scheme).HasMaxLength(30).HasColumnType("varchar(30)");
                entity.Property(e => e.Host).HasMaxLength(150).HasColumnType("varchar(150)");
                entity.Property(e => e.HostPort).HasMaxLength(200).HasColumnType("varchar(200)");
                entity.Property(e => e.Path).HasColumnType("varchar(max)");
                entity.Property(e => e.QueryString).HasMaxLength(2500).HasColumnType("varchar(2500)");
                entity.Property(e => e.Source).HasMaxLength(200).HasColumnType("varchar(200)");
                entity.Property(e => e.TraceIdentifier).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.Property(e => e.ContentType).HasMaxLength(100).HasColumnType("varchar(100)");

                entity.Property(e => e.RequestHeader)
                    .HasColumnType("varchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions))
                    .Metadata
                    .SetValueComparer(comparer);

                entity.Property(t => t.RequestBody).HasColumnType("varchar(max)");
                entity.Property(e => e.StackTrace).HasColumnType("varchar(max)");
                entity.Property(e => e.IpAddress).HasMaxLength(30).HasColumnType("varchar(30)");
                entity.Property(e => e.CreateDateOnly).HasColumnType("Date");
                entity.Property(e => e.CreateUser).HasMaxLength(40).IsUnicode(false);
                entity.Property(e => e.CreateDate).HasColumnType("DateTime2(7)");
            });

            modelBuilder.Entity<LogAudit>(entity =>
            {
                if (_reecOptions == null)
                    entity.ToTable("LogAudit");
                else if (!string.IsNullOrWhiteSpace(_reecOptions.LogAudit.Schema) &&
                         !string.IsNullOrWhiteSpace(_reecOptions.LogAudit.TableName))
                    entity.ToTable(_reecOptions.LogAudit.TableName, _reecOptions.LogAudit.Schema);
                else
                    entity.ToTable(_reecOptions.LogAudit.TableName);

                entity.HasKey(e => e.IdLogAudit);
                entity.Property(e => e.IdLogAudit).UseIdentityColumn();
                entity.Property(e => e.ApplicationName).HasColumnType("varchar(100)");
                entity.Property(e => e.HttpStatusCode).IsRequired();
                entity.Property(e => e.RequestId).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.Property(e => e.Duration).HasColumnType("time(7)");
                entity.Property(e => e.Protocol).HasMaxLength(50).HasColumnType("varchar(50)");
                entity.Property(e => e.IsHttps).IsRequired();
                entity.Property(e => e.Method).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.Property(e => e.Scheme).HasMaxLength(50).HasColumnType("varchar(50)");
                entity.Property(e => e.Host).HasMaxLength(150).HasColumnType("varchar(150)");
                entity.Property(e => e.Port).IsRequired();
                entity.Property(e => e.HostPort).HasMaxLength(200).HasColumnType("varchar(200)");
                entity.Property(e => e.Path).HasColumnType("varchar(max)");
                entity.Property(e => e.QueryString).HasMaxLength(2500).HasColumnType("varchar(2500)");
                entity.Property(e => e.TraceIdentifier).HasMaxLength(100).HasColumnType("varchar(100)");

                entity.Property(e => e.RequestHeader)
                    .HasColumnType("varchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions))
                    .Metadata
                    .SetValueComparer(comparer);

                entity.Property(e => e.RequestBody).HasColumnType("varchar(max)");

                entity.Property(e => e.ResponseHeader)
                    .HasColumnType("varchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions))
                    .Metadata
                    .SetValueComparer(comparer);

                entity.Property(e => e.ResponseBody).HasColumnType("varchar(max)");
                entity.Property(e => e.IpAddress).HasMaxLength(50).HasColumnType("varchar(50)");
                entity.Property(e => e.CreateDateOnly).HasColumnType("Date");
                entity.Property(e => e.CreateUser).HasMaxLength(40).HasColumnType("varchar(40)");
                entity.Property(e => e.CreateDate).HasColumnType("DateTime2(7)");
            });

            modelBuilder.Entity<LogEndpoint>(entity =>
            {
                if (_reecOptions == null)
                    entity.ToTable("LogEndpoint");
                else if (!string.IsNullOrWhiteSpace(_reecOptions.LogEndpoint.Schema) &&
                         !string.IsNullOrWhiteSpace(_reecOptions.LogEndpoint.TableName))
                    entity.ToTable(_reecOptions.LogEndpoint.TableName, _reecOptions.LogEndpoint.Schema);
                else
                    entity.ToTable(_reecOptions.LogEndpoint.TableName);

                entity.HasKey(e => e.IdLogEndpoint);
                entity.Property(e => e.IdLogEndpoint).UseIdentityColumn();
                entity.Property(e => e.ApplicationName).HasColumnType("varchar(100)");
                entity.Property(e => e.HttpStatusCode).IsRequired();
                entity.Property(e => e.Duration).HasColumnType("time(7)").IsRequired();
                entity.Property(e => e.TraceIdentifier).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.Property(e => e.Retry).IsRequired();
                entity.Property(e => e.Method).HasMaxLength(20).HasColumnType("varchar(20)").IsRequired();
                entity.Property(e => e.Schema).HasMaxLength(20).HasColumnType("varchar(20)").IsRequired();
                entity.Property(e => e.Host).HasMaxLength(150).HasColumnType("varchar(150)").IsRequired();
                entity.Property(e => e.Port).IsRequired();
                entity.Property(e => e.HostPort).HasMaxLength(200).HasColumnType("varchar(200)").IsRequired();
                entity.Property(e => e.Path).HasMaxLength(800).HasColumnType("varchar(800)");
                entity.Property(e => e.QueryString).HasMaxLength(1000).HasColumnType("varchar(1000)");

                entity.Property(e => e.RequestHeader)
                    .HasColumnType("varchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions))
                    .Metadata
                    .SetValueComparer(comparer);

                entity.Property(e => e.RequestBody).HasColumnType("varchar(max)");

                entity.Property(e => e.ResponseHeader)
                    .HasColumnType("varchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, serializationOptions))
                    .Metadata
                    .SetValueComparer(comparer);

                entity.Property(e => e.ResponseBody).HasColumnType("varchar(max)");
                entity.Property(e => e.CreateDateOnly).HasColumnType("Date").IsRequired();
                entity.Property(e => e.CreateUser).HasMaxLength(50).HasColumnType("varchar(50)");
                entity.Property(e => e.CreateDate).HasColumnType("DateTime2(7)").IsRequired();
            });

            modelBuilder.Entity<LogJob>(entity =>
            {
                if (_reecOptions == null)
                    entity.ToTable("LogJob");
                else if (!string.IsNullOrWhiteSpace(_reecOptions.LogJob.Schema) &&
                         !string.IsNullOrWhiteSpace(_reecOptions.LogJob.TableName))
                    entity.ToTable(_reecOptions.LogJob.TableName, _reecOptions.LogJob.Schema);
                else
                    entity.ToTable(_reecOptions.LogJob.TableName);

                entity.HasKey(e => e.IdLogJob);
                entity.Property(e => e.IdLogJob).UseIdentityColumn();
                entity.Property(e => e.ApplicationName).HasColumnType("varchar(100)");
                entity.Property(e => e.NameJob).HasMaxLength(200).HasColumnType("varchar(200)").IsRequired();
                entity.Property(t => t.StateJob)
                       .HasMaxLength(15).HasColumnType("varchar(15)")
                       .HasConversion(
                           v => v.ToString(),
                           v => (StateJob)Enum.Parse(typeof(StateJob), v));


                entity.Property(e => e.TraceIdentifier).HasMaxLength(100).HasColumnType("varchar(100)");
                entity.Property(e => e.Duration).HasColumnType("time(7)");
                entity.Property(e => e.Exception).HasColumnType("varchar(max)");
                entity.Property(e => e.InnerException).HasColumnType("varchar(max)");
                entity.Property(e => e.StackTrace).HasColumnType("varchar(max)");

                entity.Property(e => e.Data)
                    .HasColumnType("varchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, serializationOptions),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, serializationOptions))
                    .Metadata
                    .SetValueComparer(comparerObject);

                entity.Property(e => e.Message).HasMaxLength(1000).HasColumnType("varchar(1000)");
                entity.Property(e => e.CreateDateOnly).HasColumnType("Date").IsRequired();
                entity.Property(e => e.CreateUser).HasMaxLength(50).HasColumnType("varchar(50)");
                entity.Property(e => e.CreateDate).HasColumnType("DateTime2(7)").IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }

    }
}
