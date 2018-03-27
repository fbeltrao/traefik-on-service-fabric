using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StoreLocationAPI.Models;

namespace StoreLocationAPI.Controllers
{
    [Route("api/[controller]")]
    public class LocationController : Controller
    {
        private readonly StatelessServiceContext serviceContext;
        List<LocationModel> locations;

        public LocationController(StatelessServiceContext serviceContext)
        {
            this.serviceContext = serviceContext;
            this.locations = new List<LocationModel>()
            {
                new LocationModel()
                {
                    ID = 1,
                    Address = "Fakestreet 1",
                    City = "New York",
                    Country = "USA"
                },

                new LocationModel()
                {
                    ID = 2,
                    Address = "Fakestreet 102",
                    City = "London",
                    Country = "UK"
                },

                new LocationModel()
                {
                    ID = 3,
                    Address = "Fakestrasse 32",
                    City = "Zürich",
                    Country = "CH"
                }
            };
        }

        // GET api/location
        [HttpGet]
        public IEnumerable<LocationModel> Get()
        {
            return this.locations;
        }

        // GET api/location/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var existing = locations.FirstOrDefault(x => x.ID == id);
            return (existing == null) ?
                (IActionResult)NotFound() :
                Ok(existing);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // adding header 'app-version' just for demo purposes
            context.HttpContext.Response.Headers.Add("app-version", serviceContext.CodePackageActivationContext.CodePackageVersion);
            base.OnActionExecuted(context);
        }
    }
}
