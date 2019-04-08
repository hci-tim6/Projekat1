using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherHCI
{
    public class Place
    {
        public string id { get; set; }

        public string name { get; set; }

        public string country { get; set; }

        public Coordinate coord { get; set; }
    }

    public class Coordinate
    {
        public double lon { get; set; }
        public double lat { get; set; }
    }
}