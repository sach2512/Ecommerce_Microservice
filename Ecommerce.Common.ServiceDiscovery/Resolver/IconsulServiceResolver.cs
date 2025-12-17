using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Common.ServiceDiscovery.Resolver
{
    public interface IconsulServiceResolver
    {
        Task<Uri> ResolveServiceUriAsync(string servicename,CancellationToken cancellationToken);
        Task<IEnumerable<Uri>> GetHealthServiceUriAsync(string ServiceName,CancellationToken cancellationToken);
    }
}
