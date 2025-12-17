using Consul;
using Consul.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Common.ServiceDiscovery.Resolver
{
    public class consulServiceResolver : IconsulServiceResolver
    {
        private readonly IConsulClient _client;
        public consulServiceResolver(IConsulClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<Uri>> GetHealthServiceUriAsync(
     string ServiceName,
     CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
                throw new ArgumentNullException(nameof(ServiceName));

            var queryresult = await _client.Health.Service(
                ServiceName,
                "tags",
                passingOnly: true,
                cancellationToken);

            var response = queryresult.Response;
            var serviceagents = response.Select(x => x.Service).ToList();

            var listurl = new List<Uri>();

            foreach (var service in serviceagents)
            {
                // scheme from THIS service
                var scheme = "http";
                if (service.Tags != null && service.Tags.Contains("https"))
                {
                    scheme = "https";
                }

                // host + port from SAME service
                var host = service.Address;
                var port = service.Port;

                var url = new Uri($"{scheme}://{host}:{port}");
                listurl.Add(url);
            }

            return listurl;
        }








        public async Task<Uri> ResolveServiceUriAsync(string servicename, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(servicename))
            {
                return null;
            }
            var queryresult = await _client.Health.Service(servicename,tag:string.Empty,passingOnly:true, cancellationToken);
            var service = queryresult.Response; // this give instance of service netry which has services and heatlth
            if(service==null||service.Length == 0)
            {
                throw new InvalidOperationException("no service found");
            }
            var index = Random.Shared.Next(service.Length);   
            var entry = service[index];
            var serviceinfo = entry.Service;  // so we just use serce entry her withservcies like port and url
            var tags = serviceinfo.Tags;
            var scheme = tags.Any(t =>
              t.Equals("https", StringComparison.OrdinalIgnoreCase))
              ? "https"
              : "http";

            // 4️⃣ Build URI
            return new UriBuilder
            {
                Scheme = scheme,
                Host = serviceinfo.Address,
                Port = serviceinfo.Port,
                Path = "/",
                
            }.Uri;

        }
    }
}
