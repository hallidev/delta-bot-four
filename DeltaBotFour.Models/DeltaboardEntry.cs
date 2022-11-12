using System;

namespace DeltaBotFour.Models
{
    public class DeltaboardEntry
    {
        public Guid Id { get; set; }
        public string DeltaboardId { get; set; }
        public int Rank { get; set; }
        public string Username { get; set; }
        public int Count { get; set; }
        public Deltaboard Deltaboard { get; set; }
    }
}