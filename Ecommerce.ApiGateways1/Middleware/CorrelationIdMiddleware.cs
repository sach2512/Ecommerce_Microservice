using Serilog.Context;

namespace Ecommerce.ApiGateways1.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Check for existing correlation ID
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var id))
            {
                id = Guid.NewGuid().ToString("N");
                context.Request.Headers["X-Correlation-Id"] = id;
            }

            string correlationId = id.ToString();

            // Add to response for client visibility
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            // Add to Serilog log context
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);    // MUST call next middleware
            }
        }
    }
}
