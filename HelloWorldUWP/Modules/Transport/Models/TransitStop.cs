using System;
using System.Collections.Generic;

namespace HelloWorldUWP.Modules.Transport.Models
{
    public class TransitStop
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public List<string> Modes { get; set; } = new List<string>();

        public override string ToString()
        {
            return DisplayName ?? Name ?? string.Empty;
        }
    }
}
