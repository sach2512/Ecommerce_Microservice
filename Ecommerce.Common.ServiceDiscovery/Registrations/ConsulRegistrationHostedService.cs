using Consul;
using Ecommerce.Common.ServiceDiscovery.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Common.ServiceDiscovery.Registrations
{
    public class ConsulRegistrationHostedService : IHostedService
    {
        private readonly IConsulClient _consulClient;
        private readonly IConfigureOptions<ConsulConfig> _consulConfig;
        private readonly IOptions<ConsulConfig> _options;
        private readonly ILogger<ConsulRegistrationHostedService> _logger;

        public ConsulRegistrationHostedService(
                IConsulClient consulClient,
            IConfigureOptions<ConsulConfig> consulConfig,
            IOptions<ConsulConfig> options,
                 ILogger<ConsulRegistrationHostedService> logger)
        {
            _consulClient = consulClient;
            _consulConfig = consulConfig;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var serviceConfig = _options.Value;

            // Generate unique ServiceId per instance
            serviceConfig.ServiceId =
                $"{serviceConfig.ServiceName}-{Guid.NewGuid()}";

            // Parse FULL service address
            var serviceUri = new Uri(serviceConfig.ServiceAddress);

            var registration = new AgentServiceRegistration
            {
                ID = serviceConfig.ServiceId,
                Name = serviceConfig.ServiceName,
                Address = serviceUri.Host,
                Port = serviceUri.Port,
                Tags = serviceConfig.Tags
            };

            // Health check MUST be full URL
            var healthCheckUrl =
                $"{serviceUri.Scheme}://{serviceUri.Host}:{serviceUri.Port}{serviceConfig.HealthCheckEndpoint}";

            registration.Checks = new[]
            {
        new AgentServiceCheck
        {
            HTTP = healthCheckUrl,
            Interval = TimeSpan.FromSeconds(serviceConfig.HealthCheckIntervalSeconds),
            Timeout = TimeSpan.FromSeconds(serviceConfig.HealthCheckTimeoutSeconds)
        }
    };

            _logger.LogInformation(
                "Registering {ServiceName} at {Address}",
                registration.Name,
                serviceConfig.ServiceAddress);

            await _consulClient.Agent.ServiceDeregister(
                registration.ID,
                cancellationToken);

            await _consulClient.Agent.ServiceRegister(
                registration,
                cancellationToken);
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var serviceConfig = _options.Value;
            var registration = new AgentServiceRegistration { ID = serviceConfig.ServiceId };

            _logger.LogInformation($"Deregistering service from Consul: {registration.ID}");

            await _consulClient.Agent.ServiceDeregister(registration.ID, cancellationToken);
        }
    }
}
