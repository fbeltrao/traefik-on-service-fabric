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

        public void OnGet()
        {
            Message = "Where our stores are located";
            this.APIUrl = $"/api/location";
        }
    }
}
