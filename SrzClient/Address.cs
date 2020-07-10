using System;

namespace SrzClient
{
    public class Address
    {
        public string FullAddress { get; set; }
        public int? PostIndex { get; set; }
        public string House { get; set; }
        public string Building { get; set; }
        public int? Flat { get; set; }
        public string RegionPrefix { get; set; }
        public string Region { get; set; }
        public string DistrictPrefix { get; set; }
        public string District { get; set; }
        public string CityPrefix { get; set; }
        public string City { get; set; }
        public string TownPrefix { get; set; }
        public string Town { get; set; }
        public string StreetPrefix { get; set; }
        public string Street { get; set; }

        public override string ToString()
        {
            return this.PropertiesToString(Environment.NewLine);
        }
    }
}
