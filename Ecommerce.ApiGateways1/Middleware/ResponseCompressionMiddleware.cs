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
        private readonly CompressionSettings _settings;

        public ResponseCompressionMiddleware(RequestDelegate next, IOptions<CompressionSettings> settings)
        {
            _next = next;
            _settings = settings.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // If compression is globally disabled → execute pipeline normally
            if (_settings.Enabled == false)
            {
                await _next(context);
                return;
            }

            // Save original response body stream (HTTP output stream)
            var orginalbody = context.Response.Body;

            // Create a temporary memory buffer to capture ALL output
            var buffer = new MemoryStream();

            // Replace actual response stream with our buffer
            // All response content is now written into 'buffer'
            context.Response.Body = buffer;

            // Allow the rest of the pipeline to execute
            // Controller → serializer → writes into 'buffer'
            await _next(context);

            // Detect content type + client-supported encodings
            var contenttype = context.Response.ContentType;
            var acceptencoding = context.Response.Headers["Accept-Encoding"].ToString();

            // If client doesn’t support compression or content isn't compressible → send raw content
            if (string.IsNullOrEmpty(acceptencoding) || !IsCompressiblecontenttype(contenttype))
            {
                buffer.Position = 0;
                context.Response.Body = orginalbody;
                await buffer.CopyToAsync(context.Response.Body);
                return;
            }

            // Only compress content larger than threshold
            if (buffer.Length > _settings.CompressionThresholdBytes)
            {
                var encodingtypefrommethod = encodingtype(acceptencoding);

                // Reset buffer pointer to start so compression reads from beginning
                buffer.Position = 0;

                // Create a new memory stream to hold COMPRESSED bytes
                using var compressed = new MemoryStream();

                // If Brotli is chosen → compress buffer into 'compressed'
                if (encodingtypefrommethod.Equals("br", StringComparison.OrdinalIgnoreCase))
                {
                    // BrotliStream writes COMPRESSED DATA into 'compressed'
                    using (var brotli = new BrotliStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        // Copy UNCOMPRESSED buffer → into Brotli → which writes COMPRESSED output
                        await buffer.CopyToAsync(brotli);
                    }
                }
                // If gzip is chosen → compress buffer into 'compressed'
                else if (encodingtypefrommethod.Equals("gzip", StringComparison.OrdinalIgnoreCase))
                {
                    using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        await buffer.CopyToAsync(gzip);
                    }
                }
                else
                {
                    // Unsupported encoding → send uncompressed response
                    buffer.Position = 0;
                    context.Response.Body = orginalbody;
                    await buffer.CopyToAsync(context.Response.Body);
                    return;
                }

                // Tell the browser the response is compressed (gzip or br)
                context.Response.Headers["Content-Encoding"] = encodingtypefrommethod;

                // Set correct compressed payload size
                context.Response.ContentLength = compressed.Length;

                // Reset pointer so we can read compressed bytes from start
                compressed.Position = 0;

                // Restore the original HTTP response stream
                context.Response.Body = orginalbody;

                // Write COMPRESSED bytes to actual output stream
                await compressed.CopyToAsync(context.Response.Body);
            }
            else
            {
                // If below threshold → send raw (uncompressed) response
                buffer.Position = 0;
                context.Response.Body = orginalbody;
                await buffer.CopyToAsync(context.Response.Body);
            }
        }

        private string encodingtype(string acceptencoding)
        {
            // Decide which compression type to use based on client request + supported list
            if (acceptencoding.Contains("br", StringComparison.OrdinalIgnoreCase) &&
                _settings.SupportedEncoding.Contains("br"))
            {
                return "br";
            }

            if (acceptencoding.Contains("gzip", StringComparison.OrdinalIgnoreCase) &&
                _settings.SupportedEncoding.Contains("gzip"))
            {
                return "gzip";
            }

            // Fallback
            return _settings.DefaultEncoding;
        }

        private bool IsCompressiblecontenttype(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            // Only compress compressible MIME types
            return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase) ||
                   contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
        }
    }
}
