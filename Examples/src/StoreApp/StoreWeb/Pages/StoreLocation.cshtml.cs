using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json.Linq;

namespace StoreWeb.Pages
{
    public class StoreLocationModel : PageModel
    {
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public StoreLocationModel(FabricClient fabricClient, StatelessServiceContext serviceContext)
        {
            this.fabricClient = fabricClient;
            this.serviceContext = serviceContext;
        }

        public string Message { get; set; }

        public string APIUrl { get; set; }

        public async Task OnGet()
        {
            Message = "Where our stores are located";

            // Resolve APIUrl for client side communication
            // THIS IS DEMO QUALITY CODE!!!
            // This is simple way to communicate with another service in same application
            // Production code should follow the pattern in this documentation:
            // https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-communication#communicating-with-a-service
            ServicePartitionResolver resolver = new ServicePartitionResolver(() => fabricClient);

            var cts = new CancellationToken();
            var partition = await resolver.ResolveAsync(new Uri($"{serviceContext.CodePackageActivationContext.ApplicationName}/StoreLocationAPI"), new ServicePartitionKey(), cts);

            var addresses = JObject.Parse(partition.Endpoints.FirstOrDefault().Address);
            var primaryReplicaAddress = (string)addresses["Endpoints"].First;
            this.APIUrl = $"{primaryReplicaAddress}/api/location";
        }
    }
}
