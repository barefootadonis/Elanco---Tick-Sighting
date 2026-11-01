using System;

namespace Elanco___Tick_Sighting.Models
{
    public class TickSighting
    {
        public int TickSightingId { get; set; }

        public string City { get; set; } = string.Empty;
        public DateTime SightingDate { get; set; }

        public string Species { get; set; } = string.Empty;
        public string? LatinName { get; set; }
        public string? ImagePath { get; set; }

        public string? Description { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}