using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeltaBotFour.Models
{
    public class DeltaboardEntry
    {
        public Guid Id { get; set; }
        public string DeltaboardId { get; set; }
        public string Username { get; set; }
        public int Count { get; set; }
        [NotMapped] public int Rank { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public Deltaboard Deltaboard { get; set; }
    }
}