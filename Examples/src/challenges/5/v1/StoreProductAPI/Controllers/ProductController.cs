using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StoreProductAPI.Models;

namespace StoreProductAPI.Controllers
{
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly StatelessServiceContext serviceContext;
        List<ProductModel> products;

        public ProductController(StatelessServiceContext serviceContext)
        {
            this.serviceContext = serviceContext;
            this.products = new List<ProductModel>()
            {
                new ProductModel()
                {
                    ID = 1000,
                    Name = "Super Shoes",
                    Description = "Super shoes, water resistent",
                    Price = 80
                },

                new ProductModel()
                {
                    ID = 1001,
                    Name = "Super Jacket",
                    Description = "Super Jacket, water resistent",
                    Price = 120
                },

                new ProductModel()
                {
                    ID = 1003,
                    Name = "Super Pants",
                    Description = "Super pants, water resistent",
                    Price = 90
                }
            };
        }


        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // adding header 'app-version' just for demo purposes
            context.HttpContext.Response.Headers.Add("app-version", serviceContext.CodePackageActivationContext.CodePackageVersion);
            base.OnActionExecuted(context);
        }

        // GET api/product
        [HttpGet]
        public IEnumerable<ProductModel> Get()
        {
            return this.products;
        }

        // GET api/product/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var existing = products.FirstOrDefault(x => x.ID == id);
            return (existing == null) ?
                (IActionResult)NotFound() :
                Ok(existing);
        }
    }
}
