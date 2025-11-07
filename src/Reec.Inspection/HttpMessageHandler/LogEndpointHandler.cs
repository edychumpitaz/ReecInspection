using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Reec.Inspection.Entities;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using System.Diagnostics;

namespace Reec.Inspection.HttpMessageHandler
{
    public class LogEndpointHandler : DelegatingHandler

    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly ReecExceptionOptions _reecException;
        private readonly IDateTimeService _dateTime;

        public LogEndpointHandler(IServiceScopeFactory serviceScope, ReecExceptionOptions reecException, IDateTimeService dateTime)
        {
            this._serviceScope = serviceScope;
            this._reecException = reecException;
            this._dateTime = dateTime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_reecException.LogEndpoint.IsSaveDB)            
                return await base.SendAsync(request, cancellationToken);
            
            using var scope = _serviceScope.CreateScope();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            var ContextService = scope.ServiceProvider.GetRequiredService<IDbContextService>();
            var dbContext = ContextService.GetDbContext();

            Stopwatch stopwatch = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var retryKey = new HttpRequestOptionsKey<int>("RetryAttempts");
            request.Options.TryGetValue(retryKey, out var retry);

            var traceIdentifier = httpContextAccessor.HttpContext?.TraceIdentifier ?? null;
            var entity = new LogEndpoint()
            {
                ApplicationName = _reecException.ApplicationName,
                CreateDateOnly = DateOnly.FromDateTime(_dateTime.Now),
                CreateDate = _dateTime.Now,
                Duration = stopwatch.Elapsed,
                Host = response.RequestMessage.RequestUri.Host,
                HostPort = $"{response.RequestMessage.RequestUri.Host}:{response.RequestMessage.RequestUri.Port}",
                HttpStatusCode = (int)response.StatusCode,
                Method = response.RequestMessage.Method.Method,
                Path = response.RequestMessage.RequestUri.AbsolutePath,
                Port = response.RequestMessage.RequestUri.Port,
                Schema = response.RequestMessage.RequestUri.Scheme,
                TraceIdentifier = traceIdentifier,
                Retry = Convert.ToByte(retry)
            };

            var queryString = response.RequestMessage.RequestUri.Query;
            if (!string.IsNullOrWhiteSpace(queryString))
                entity.QueryString = response.RequestMessage.RequestUri.Query;


            entity.RequestHeader = response.RequestMessage.Headers
                                    .Select(t => new { t.Key, Value = string.Join(",", t.Value) })
                                    .ToDictionary(t => t.Key, t => t.Value);
            if (response.RequestMessage.Content is not null)
                entity.RequestBody = await response.RequestMessage.Content.ReadAsStringAsync(default);


            entity.ResponseHeader = response.Headers
                                    .Select(t => new { t.Key, Value = string.Join(",", t.Value) })
                                    .ToDictionary(t => t.Key, t => t.Value);

            if (response.Content is not null)
                entity.ResponseBody = await response.Content.ReadAsStringAsync(default);

            await dbContext.LogEndpoints.AddAsync(entity, default);
            await dbContext.SaveChangesAsync(default);
            entity = null;
            stopwatch = null;
            return response;
        }

    }
}
