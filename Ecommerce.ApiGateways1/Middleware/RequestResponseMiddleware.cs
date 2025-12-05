using System.Text;

namespace Ecommerce.ApiGateways1.Middleware
{
    public class RequestResponseMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly List<String> DefaultSensitiveKeys = new List<string>()
        {
            "password", "pwd", "token", "secret", "api_key", "apikey", "token",
            "accesstoken", "refreshtoken", "access_token", "refresh_token"

        };


        private const int MaxBodySize = 64 * 1024; 


        public RequestResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;

            bool hasBody = request.ContentLength.HasValue && request.ContentLength > 0;

            bool isJson = request.ContentType != null &&
                          request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

            bool isWriteMethod =
                request.Method == HttpMethods.Post ||
                request.Method == HttpMethods.Put ||
                request.Method == HttpMethods.Patch;

            if (request != null && hasBody && isJson && isWriteMethod)
            {
                // now the request is validated
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var raw = reader.ReadToEnd();
                context.Request.Body.Position = 0;
                string requestBody;

                if (raw.Length <= MaxBodySize)
                {
                    requestBody = MaskSensitiveData(raw);
                }
                else
                {
                    requestBody = $"[Request body too large: {raw.Length / 1024} KB truncated]";
                }

            }

          

            await _next(context);
        }

        private string MaskSensitiveData(string raw)
        {
            throw new NotImplementedException();
        }
    }
}
