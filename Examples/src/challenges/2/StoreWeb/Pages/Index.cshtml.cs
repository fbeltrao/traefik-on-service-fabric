using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StoreWeb.Pages
{
    public class IndexModel : PageModel
    {
        static HttpClient httpClient = new HttpClient();
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public JArray Products { get; internal set; }

        public IndexModel( FabricClient fabricClient, StatelessServiceContext serviceContext)
        {
            this.fabricClient = fabricClient;
            this.serviceContext = serviceContext;
        }

        public async Task OnGet()
        {
            this.Products = await GetData();
        }

        async Task<JArray> GetData()
        {
            // THIS IS DEMO QUALITY CODE!!!
            // This is simple way to communicate with another service in same application
            // Production code should follow the pattern in this documentation:
            // https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-communication#communicating-with-a-service
            ServicePartitionResolver resolver = new ServicePartitionResolver(() => fabricClient);

            var cts = new CancellationToken();
            var partition = await resolver.ResolveAsync(new Uri($"{serviceContext.CodePackageActivationContext.ApplicationName}/StoreProductAPI"), new ServicePartitionKey(), cts);

            var addresses = JObject.Parse(partition.Endpoints.FirstOrDefault().Address);
            var primaryReplicaAddress = (string)addresses["Endpoints"].First;
            using (var response = await httpClient.GetAsync($"{primaryReplicaAddress}/api/product"))
            {
                if (response.IsSuccessStatusCode)
                {
                    return (JArray)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                }
            }

            return new JArray();
        }
    }
}
