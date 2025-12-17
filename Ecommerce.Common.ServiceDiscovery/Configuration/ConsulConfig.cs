using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Common.ServiceDiscovery.Configuration
{
    public class ConsulConfig
    {

        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceAddress { get; set; } // FULL URL
        public string HealthCheckEndpoint { get; set; } = "/health";
        public int HealthCheckIntervalSeconds { get; set; } = 10;
        public int HealthCheckTimeoutSeconds { get; set; } = 5;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
