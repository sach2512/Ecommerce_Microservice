using Ecommerce.ApiGateways1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text;

namespace Ecommerce.ApiGateways1.Middleware
{
    public class ResponseCompressionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly  CompressionSettings _settings;

        public ResponseCompressionMiddleware(RequestDelegate next, IOptions<CompressionSettings> settings)
        {
            _next = next;
            _settings=settings.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_settings.Enabled == false)
            {
                await _next(context);
                return;
            }
            var orginalbody = context.Response.Body;
            var buffer = new MemoryStream();
            context.Response.Body = buffer;
            await _next(context);

            var contenttype = context.Response.ContentType;
            var acceptencoding = context.Response.Headers["Accept-Encoding"].ToString();
            if (string.IsNullOrEmpty(acceptencoding) || !IsCompressiblecontenttype(contenttype))
            {
                buffer.Position = 0;
                context.Response.Body=orginalbody;
                await buffer.CopyToAsync(context.Response.Body);
                return;
            }
            if (buffer.Length > _settings.CompressionThresholdBytes)
            {
               var encodingtypefrommethod = encodingtype(acceptencoding);
                buffer.Position = 0;
                using var compressed = new MemoryStream();

                if (encodingtypefrommethod.Equals("br", StringComparison.OrdinalIgnoreCase))
                {
                    using (var brotli = new BrotliStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        await buffer.CopyToAsync(brotli);
                    }
                }
                // Gzip: widely supported, safe fallback.
                else if (encodingtypefrommethod.Equals("gzip", StringComparison.OrdinalIgnoreCase))
                {
                    using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        await buffer.CopyToAsync(gzip);
                    }
                }
                else
                {
                    // If we end up with an encoding we don't handle,
                    // fall back to sending the original uncompressed response.
                    buffer.Position = 0;
                    context.Response.Body = orginalbody;
                    await buffer.CopyToAsync(context.Response.Body);
                    return;
                }
                context.Response.Headers["Content-Encoding"] = encodingtypefrommethod;
                context.Response.ContentLength = compressed.Length;

                // 9. Write compressed payload to the real response stream.
                compressed.Position = 0;
                context.Response.Body = orginalbody;
                await compressed.CopyToAsync(context.Response.Body);
            }
            else
            {
                // 10. Below threshold: send as-is without compression.
                // Restores original stream and copies buffered content.
                buffer.Position = 0;
                context.Response.Body = orginalbody;
                await buffer.CopyToAsync(context.Response.Body);
            }


        }

        
        

        private string encodingtype(string acceptencoding)
        {
            if (acceptencoding.Contains("br", StringComparison.OrdinalIgnoreCase) && _settings.SupportedEncoding.Contains("br"))
            {
                return "br";
            }
            if (acceptencoding.Contains("br", StringComparison.OrdinalIgnoreCase) && _settings.SupportedEncoding.Contains("br"))
            {
                return "gzip";
            } 
            return _settings.DefaultEncoding;
        }
        private bool IsCompressiblecontenttype(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            // Allow only compressible MIME types
            return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

        }
    }
}
