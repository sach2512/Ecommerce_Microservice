using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using System.Net;

namespace Ecommerce.ApiGateways1.Middleware
{
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _scheme;

        public JwtAuthenticationMiddleware(RequestDelegate next, string scheme = JwtBearerDefaults.AuthenticationScheme)
        {
            _next = next;
            _scheme = scheme;
        }

        public async Task Invoke(HttpContext context, IAuthenticationService auth)
        {
            var authHeader = context.Request.Headers["X-Authorization"].ToString();

            if (!string.IsNullOrWhiteSpace(authHeader) &&
                authHeader.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                var result = await auth.AuthenticateAsync(context, _scheme);

                if (!result.Succeeded)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers["WWW-Authenticate"] = "Bearer error=\"invalid_token\"";
                    context.Response.ContentType = "application/json";

                    var problemDetails = new
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Details = "Invalid token",
                        Timestamp = DateTime.UtcNow
                    };

                    if (!context.Response.HasStarted)
                    {
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(problemDetails));
                    }

                    return; 
                }

                context.User = result.Principal;
            }

            await _next(context);
        }
    }
}
