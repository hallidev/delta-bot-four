using System;
using System.Collections.Generic;

namespace DeltaBotFour.Models
{
    public class Deltaboard
    {
        public DeltaboardType DeltaboardType { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
        public List<DeltaboardEntry> Entries { get; set; }
    }
}
