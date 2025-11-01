using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Reec.Inspection;
using Reec.Inspection.Entities;
using Reec.Inspection.Options;
using System.Diagnostics;
using System.Net;

namespace BaseArchitecture.Api.Middleware
{
    public class LogAuditMiddleware<TDbContext> : IMiddleware
                                where TDbContext : InspectionDbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ReecExceptionOptions _exceptionOptions;
        private readonly ILogger<LogAuditMiddleware<TDbContext>> _logger;

        public LogAuditMiddleware(ILogger<LogAuditMiddleware<TDbContext>> logger,
                                    TDbContext dbContext, ReecExceptionOptions exceptionOptions)
        {
            this._dbContext = dbContext;
            this._exceptionOptions = exceptionOptions;
            this._logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {

            if (context.Request.Path.HasValue &&
                !context.Request.Path.Value.Contains("swagger") &&
                !context.Request.Path.Value.Contains("index") &&
                !context.Request.Path.Value.Contains("favicon") &&
                context.Request.Method != HttpMethod.Options.Method)
            {

                context.Request.EnableBuffering();

                var requestHeader = context.Request.Headers
                                    .Select(t => new { t.Key, Value = string.Join<string>(",", t.Value) })
                                    .ToDictionary(t => t.Key, t => t.Value);

                Stopwatch stopwatch = Stopwatch.StartNew();
                var entity = new LogAudit
                {
                    ApplicationName = _exceptionOptions.ApplicationName,
                    CreateDate = DateTime.Now,
                    CreateDateOnly = DateOnly.FromDateTime(DateTime.Now),
                    Host = context.Request.Host.Host,
                    Port = context.Request.Host.Port.GetValueOrDefault(),
                    HostPort = context.Request.Host.Value,
                    Path = context.Request.Path,
                    IpAddress = context.Connection.RemoteIpAddress.ToString(),
                    IsHttps = context.Request.IsHttps,
                    Method = context.Request.Method,
                    Scheme = context.Request.Scheme,
                    TraceIdentifier = context.TraceIdentifier,
                    Protocol = context.Request.Protocol,
                    RequestHeader = requestHeader,
                    RequestId = context.TraceIdentifier
                };

                var queryString = context.Request.QueryString.ToString();
                if (!string.IsNullOrWhiteSpace(queryString))
                    entity.QueryString = queryString;

                //using var sr = new StreamReader(context.Request.Body);
                //var requestBody = await sr.ReadToEndAsync();

                //context.Request.Body.Seek(0, SeekOrigin.Begin);
                //if (!string.IsNullOrWhiteSpace(requestBody))
                //    entity.RequestBody = requestBody;

                if (context.Request.Body.Length > 0 && 
                    context.Request.Body.Length <= _exceptionOptions.LogAudit.RequestBodyMaxSize)
                {
                    using var sr = new StreamReader(context.Request.Body);
                    entity.RequestBody = await sr.ReadToEndAsync();
                    context.Request.Body.Seek(0, SeekOrigin.Begin);
                }

                //Almacenar el flujo de cuerpo original para restaurar el cuerpo de respuesta a su flujo original.
                var originalBodyStream = context.Response.Body;
                var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                //Ejecutar el siguiente middleware.
                await next(context);

                var responseBodyText = "";
                if (responseBodyStream.Length > 0 &&
                    responseBodyStream.Length <= _exceptionOptions.LogAudit.ResponseBodyMaxSize)
                {
                    // Restablecer la posición a 0 después de leer
                    responseBodyStream.Seek(0, SeekOrigin.Begin);

                    using var streamReader = new StreamReader(responseBodyStream);
                    responseBodyText = await streamReader.ReadToEndAsync();
                }

                context.Response.Body = originalBodyStream;
                await context.Response.Body.WriteAsync(responseBodyStream.ToArray());

                var responseHeader = context.Response.Headers
                                    .Select(t => new { t.Key, Value = string.Join<string>(",", t.Value) })
                                    .ToDictionary(t => t.Key, t => t.Value);

                entity.ResponseHeader = responseHeader;
                if (!string.IsNullOrWhiteSpace(responseBodyText))
                    entity.ResponseBody = responseBodyText;

                stopwatch.Stop();
                entity.HttpStatusCode = (HttpStatusCode)context.Response.StatusCode;
                entity.Duration = stopwatch.Elapsed;
                _dbContext.LogAudits.Add(entity);
                await _dbContext.SaveChangesAsync();

                requestHeader.Clear();
                requestHeader = null;
                //requestBody = null;
                responseBodyText = null;
                responseHeader.Clear();
                responseHeader = null;
                entity = null;
            }
            else
                await next(context);


        }
    }
}
