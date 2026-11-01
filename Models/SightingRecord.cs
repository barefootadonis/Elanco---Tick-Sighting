namespace Elanco___Tick_Sighting.Models
{
    public class SightingRecord
    {
        public string Id { get; set; } = "";
        public DateTime Date { get; set; }
        public string Location { get; set; } = "";
        public string Species { get; set; } = "";
        public string? LatinName { get; set; }
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public string Source { get; set; } = "";
    }

    public class SightingResult
    {
        public List<SightingRecord> Sightings { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}