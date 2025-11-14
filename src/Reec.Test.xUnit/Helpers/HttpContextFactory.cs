using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;

namespace Reec.Test.xUnit.Helpers
{
    /// <summary>
    /// Factory para crear HttpContext mockeado para testing de middlewares
    /// </summary>
    public static class HttpContextFactory
    {
        /// <summary>
        /// Crea un HttpContext completo con request, response y servicios mockeados
        /// </summary>
        public static DefaultHttpContext CreateHttpContext(
            string path = "/test",
            string method = "GET",
            string? requestBody = null,
            Dictionary<string, string>? headers = null,
            ClaimsPrincipal? user = null)
        {
            var context = new DefaultHttpContext();

            // Request configuration
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("localhost", 5000);
            context.Request.Protocol = "HTTP/1.1";
            context.Request.ContentType = "application/json";

            // Headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    context.Request.Headers[header.Key] = header.Value;
                }
            }

            // Request body
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                var bytes = Encoding.UTF8.GetBytes(requestBody);
                context.Request.Body = new MemoryStream(bytes);
                context.Request.ContentLength = bytes.Length;
            }

            // Response
            context.Response.Body = new MemoryStream();

            // User authentication
            if (user != null)
            {
                context.User = user;
            }

            // Connection info
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            // TraceIdentifier
            context.TraceIdentifier = Guid.NewGuid().ToString();

            return context;
        }

        /// <summary>
        /// Crea un usuario autenticado para testing
        /// </summary>
        public static ClaimsPrincipal CreateAuthenticatedUser(string username = "testuser", params string[] roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Lee el contenido del response body
        /// </summary>
        public static async Task<string> ReadResponseBodyAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return await reader.ReadToEndAsync();
        }
    }
}
