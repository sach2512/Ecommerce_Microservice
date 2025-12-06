using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.ApiGateways1.Middleware
{
    public class ResponseCachingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _config;

        public ResponseCachingMiddleware(RequestDelegate next, IDistributedCache cache, IConfiguration config)
        {
            _next = next;
            _cache = cache;
            _config = config;
        }


        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Get || !_config.GetValue<bool>("ReddisCacheSetting:Enabled")) {
                await _next(context);
                return;
            }
            var reqPath = context.Request.Path.Value?.ToLower() ?? "";
            var cachedPolicies = _config.GetSection("ReddisCacheSetting:CachePolicies")
             .Get<Dictionary<string, int>>() ?? new Dictionary<string, int>();

            var matchedpolicy = cachedPolicies.FirstOrDefault(x => reqPath.StartsWith(x.Key.ToLowerInvariant()));
            if (matchedpolicy.Key == null)
            {
                await _next(context);
                return;
            }
            // generate a key 
            var method = context.Request.Method.ToUpperInvariant();

            var queryParams = context.Request.Query
            .OrderBy(x => x.Key)
            .Select(q => $"{q.Key.ToLowerInvariant()}={q.Value}")
                .ToList();

            var cachedkey = $"{ reqPath} {method}{queryParams}";
            var cachedvalue = await _cache.GetStringAsync(cachedkey);
            // cachedvalue just doesnt return key but return alredy cached resposn eavaible in reddis
            if (!string.IsNullOrEmpty(cachedvalue))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(cachedvalue);
                // we are writing that to response 
                return;
               
            }
            // if its not alredy cached cache it for that we need ttl
            var ttl = matchedpolicy.Value > 0
      ? matchedpolicy.Value
      : _config.GetValue<int>("ReddisCacheSetting:DefaultCacheDurationInSeconds");

            var orginalbody = context.Response.Body;
            var buffer = new MemoryStream();
            context.Response.Body = buffer;
            await _next(context);
            buffer.Position = 0;
            var readingbuffer = await new StreamReader(buffer).ReadToEndAsync();
            await _cache.SetStringAsync(
                          cachedkey,
                      readingbuffer,
                  new DistributedCacheEntryOptions
                   {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl)
                    });

            buffer.Position = 0;
           await  buffer.CopyToAsync(orginalbody);

            context.Response.Body = orginalbody;


        }
    }
}
