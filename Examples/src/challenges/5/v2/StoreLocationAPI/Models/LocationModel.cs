using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StoreLocationAPI.Models
{
    public class LocationModel
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public int ID { get; set; }
        public string Continent { get; set; }
    }
}
