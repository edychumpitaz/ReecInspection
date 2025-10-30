using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Reec.Inspection.Entities;
using Reec.Inspection.Extensions;
using Reec.Inspection.Options;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection.Middlewares
{
    public class LogHttpMiddleware<TDbContext> : IMiddleware
                                    where TDbContext : InspectionDbContext
    {
        private readonly ILogger<LogHttpMiddleware<TDbContext>> _logger;
        private readonly TDbContext _dbContext;
        private readonly ReecExceptionOptions _reecOptions;

        public LogHttpMiddleware(ILogger<LogHttpMiddleware<TDbContext>> logger,
                                        TDbContext dbContext,
                                        ReecExceptionOptions reecOptions
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _reecOptions = reecOptions;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                /*                 
                var tamanioMinimo = 1024 * 45; //Kb
                var tamanioMaximo = 1024 * 1024 * 15; //15Mb
                context.Request.EnableBuffering(tamanioMinimo, tamanioMaximo);
                */
                if (_reecOptions.EnableBuffering)
                    context.Request.EnableBuffering();

                await next(context);
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await TryHandleAsync(context, ex, stopwatch.Elapsed);
            }
        }


        public async ValueTask TryHandleAsync(HttpContext httpContext, Exception exception, TimeSpan Duration)
        {
            try
            {
                httpContext.Response.ContentType = "application/json";
                ReecMessage reecMessage;
                LogHttp logHttp;
                ReecException reecException;
                string exceptionMessage = null;

                if (exception.GetType() == typeof(ReecException))
                {
                    reecException = (ReecException)exception;
                    reecMessage = reecException.ReecMessage;
                    exceptionMessage = reecException.ExceptionMessage;

                    switch (reecMessage.Category)
                    {
                        case Category.Warning:
                        case Category.BusinessLogic:
                        case Category.BusinessLogicLegacy:
                            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                            break;
                        default:
                            httpContext.Response.StatusCode = (int)reecMessage.Category; break;
                    }
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    reecMessage = new ReecMessage(Category.InternalServerError, _reecOptions.InternalServerErrorMessage);
                }

                //Obtenemos datos de Error para guardarlo
                logHttp = await ErrorControlado(reecMessage, httpContext, exception, exceptionMessage);
                logHttp.Duration = Duration;
                reecMessage.Path = logHttp.Path;
                reecMessage.TraceIdentifier = logHttp.TraceIdentifier;


                if (reecMessage.Category >= _reecOptions.MinCategory)
                {
                    await _dbContext.LogHttp.AddAsync(logHttp);
                    var vResult = await _dbContext.SaveChangesAsync();
                    if (vResult > 0)
                        reecMessage.Id = logHttp.IdLogHttp;

                    var message = string.Join("\n\r", reecMessage.Message);
                    if (reecMessage.Category >= Category.InternalServerError)
                        _logger.LogError(exception, message);
                    else
                        _logger.LogWarning(message);
                }

                logHttp = null;
                if (!_reecOptions.EnableProblemDetails)
                    await httpContext.Response.WriteAsJsonAsync(reecMessage);
                else
                {
                    httpContext.Response.Headers.Append("EnableProblemDetails", "true");
                    var problemDetails = new ProblemDetails
                    {
                        Title = reecMessage.CategoryDescription.AddSpacesToCamelCase(),
                        Status = httpContext.Response.StatusCode,
                        Detail = string.Join("\n\r", reecMessage.Message),
                        Instance = reecMessage.Path,
                        Extensions = {
                            { "id", reecMessage.Id },
                            { "category", reecMessage.Category },
                            { "traceIdentifier", reecMessage.TraceIdentifier }
                        }
                    };
                    await httpContext.Response.WriteAsJsonAsync(problemDetails);
                }

            }
            catch (Exception ex)
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                _logger.LogError(ex, _reecOptions.ApplicationErrorMessage);

                if (!_reecOptions.EnableProblemDetails)
                {
                    var reecMessage = new ReecMessage(Category.InternalServerError, _reecOptions.ApplicationErrorMessage, httpContext.Request.Path.Value)
                    { TraceIdentifier = httpContext.TraceIdentifier };
                    await httpContext.Response.WriteAsJsonAsync(reecMessage);
                }
                else
                {
                    httpContext.Response.Headers.Append("EnableProblemDetails", "true");
                    var problemDetails = new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Status = httpContext.Response.StatusCode,
                        Detail = _reecOptions.ApplicationErrorMessage,
                        Instance = httpContext.Request.Path.Value,
                        Extensions = {
                            { "id", 0 },
                            { "category", Category.InternalServerError },
                            { "traceIdentifier", httpContext.TraceIdentifier }
                        }
                    };
                    await httpContext.Response.WriteAsJsonAsync(problemDetails);
                }
            }

        }


        private async Task<LogHttp> ErrorControlado(ReecMessage reecMessage, HttpContext httpContext, Exception ex, string exceptionMessage)
        {

            string requestBody = null;
            if (httpContext.Request.Body.CanRead)
            {
                httpContext.Request.Body.Position = 0;
                //Aquí obtenemos la información que envio el cliente al servidor.
                if (httpContext.Request.Body.Length > 0)
                {
                    using var sr = new StreamReader(httpContext.Request.Body);
                    requestBody = await sr.ReadToEndAsync();
                }
            }

            var serializationOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            LogHttp logHttp = new()
            {
                ApplicationName = _reecOptions.ApplicationName,
                Category = reecMessage.Category,
                CategoryDescription = reecMessage.Category.ToString(),
                HttpStatusCode = (HttpStatusCode)httpContext.Response.StatusCode,
                MessageUser = JsonSerializer.Serialize(reecMessage.Message, serializationOptions),
                Path = httpContext.Request.Path.Value,
                Method = httpContext.Request.Method,
                Host = httpContext.Request.Host.Host,
                Port = httpContext.Request.Host.Port.GetValueOrDefault(),
                HostPort = httpContext.Request.Host.Value,
                Source = ex.Source,
                StackTrace = ex.StackTrace,
                RequestBody = requestBody,
                ExceptionMessage = exceptionMessage,
                CreateDate = DateTime.Now,
                RequestId = httpContext.TraceIdentifier,
                TraceIdentifier = httpContext.TraceIdentifier,
                ContentType = httpContext.Request.ContentType,
                Protocol = httpContext.Request.Protocol,
                Scheme = httpContext.Request.Scheme,
                IsHttps = httpContext.Request.IsHttps,
                IpAddress = httpContext.Connection.RemoteIpAddress.ToString(),
                CreateDateOnly = DateOnly.FromDateTime(DateTime.Now),
            };


            if (httpContext.Request.QueryString.HasValue)
                logHttp.QueryString = httpContext.Request.QueryString.Value;

            if (httpContext.User.Identity.IsAuthenticated)
                logHttp.CreateUser = httpContext.User.Identity.Name; 

            if (reecMessage.Category != Category.InternalServerError && string.IsNullOrWhiteSpace(exceptionMessage))
                logHttp.StackTrace = null;
            else if (reecMessage.Category == Category.InternalServerError)
                logHttp.ExceptionMessage = ex.Message;

            if (ex.InnerException != null)
                logHttp.InnerExceptionMessage = ex.InnerException.Message;

            if (!string.IsNullOrWhiteSpace(_reecOptions.LogHttp.IpAddressFromHeader))
            {
                var ipAddress = httpContext.Request.Headers.FirstOrDefault(t => string.Equals(t.Key, _reecOptions.LogHttp.IpAddressFromHeader, StringComparison.OrdinalIgnoreCase));
                logHttp.IpAddress = ipAddress.Value;
            }

            if (!string.IsNullOrWhiteSpace(_reecOptions.LogHttp.RequestIdFromHeader))
            {
                var requestId = httpContext.Request.Headers.FirstOrDefault(t => string.Equals(t.Key, _reecOptions.LogHttp.RequestIdFromHeader, StringComparison.OrdinalIgnoreCase));
                logHttp.IpAddress = requestId.Value;
            }

            var header = httpContext.Request.Headers.Select(t => new { t.Key, Value = t.Value.ToString() });
            if (_reecOptions.LogHttp.HeaderKeysInclude != null && _reecOptions.LogHttp.HeaderKeysInclude.Count > 0)
            {
                header = (from a in header
                          join b in _reecOptions.LogHttp.HeaderKeysInclude
                          on a.Key equals b
                          select a).ToList();
            }
            else if (_reecOptions.LogHttp.HeaderKeysExclude != null && _reecOptions.LogHttp.HeaderKeysExclude.Count > 0)
            {
                var exclude = (from a in header
                               join b in _reecOptions.LogHttp.HeaderKeysExclude
                               on a.Key equals b
                               select a).ToList();
                header = header.Except(exclude).ToList();
            }

            logHttp.RequestHeader = header.ToDictionary(t => t.Key, t => t.Value); 
            return logHttp;
        }

    }
}
