using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Reec.Inspection.Entities;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using System.Diagnostics;
using System.Net;

namespace Reec.Inspection.Middlewares
{
    public class LogAuditMiddleware : IMiddleware
    {
        private readonly InspectionDbContext _dbContext;
        private readonly ReecExceptionOptions _reecOptions;
        private readonly IDateTimeService _dateTime;
        private readonly ILogger<LogAuditMiddleware> _logger;

        public LogAuditMiddleware(ILogger<LogAuditMiddleware> logger,
                                    IDbContextService dbContextService,
                                    ReecExceptionOptions reecOptions,
                                    IDateTimeService dateTime)
        {
            this._dbContext = dbContextService.GetDbContext();
            this._reecOptions = reecOptions;
            this._dateTime = dateTime;
            this._logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {

            if (!ExcludePaths(context))
            {
                await next(context);
                return;
            }

            if (_reecOptions.LogAudit.EnableBuffering)
                context.Request.EnableBuffering();

            string requestBody = null;
            if (context.Request.Body.Length < 0 &&
                context.Request.Body.Length <= _reecOptions.LogAudit.RequestBodyMaxSize)
            {
                using var sr = new StreamReader(context.Request.Body);
                requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
            }
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            var requestHeader = context.Request.Headers
                                .Select(t => new { t.Key, Value = string.Join<string>(",", t.Value) })
                                .ToDictionary(t => t.Key, t => t.Value);

            Stopwatch stopwatch = Stopwatch.StartNew();
            var entity = new LogAudit
            {
                ApplicationName = _reecOptions.ApplicationName,
                CreateDate = _dateTime.Now,
                CreateDateOnly = DateOnly.FromDateTime(_dateTime.Now),
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

            if (!string.IsNullOrWhiteSpace(requestBody))
                entity.RequestBody = requestBody;

            //Almacenar el flujo de cuerpo original para restaurar el cuerpo de respuesta a su flujo original.
            var originalBodyStream = context.Response.Body;
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            //Ejecutar el siguiente middleware.
            await next(context);

            string responseBodyText = null;
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            if (responseBodyStream.Length < 0 &&
                responseBodyStream.Length <= _reecOptions.LogAudit.ResponseBodyMaxSize)
            {
                using var streamReader = new StreamReader(responseBodyStream);
                responseBodyText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
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
            requestBody = null;
            responseBodyText = null;
            responseHeader.Clear();
            responseHeader = null;
            entity = null;

        }

        private bool ExcludePaths(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var result = context.Request.Path.HasValue && !_reecOptions.LogAudit.ExcludePaths.Any(x => path.Contains(x, StringComparison.InvariantCultureIgnoreCase));
            return result;
        }
    }
}
