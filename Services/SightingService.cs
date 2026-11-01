using Elanco___Tick_Sighting.Data;
using Elanco___Tick_Sighting.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;

namespace Elanco___Tick_Sighting.Services
{
    public class SightingService
    {
        private readonly HttpClient _http;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SightingService> _logger;

        private class ExternalBatch
        {
            public List<SightingRecord> Records { get; set; } = new();
            public int Skipped { get; set; }
        }

        public SightingService(
            HttpClient http,
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<SightingService> logger)
        {
            _http = http;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SightingResult> GetAsync(
            string? city = null,
            DateTime? from = null,
            DateTime? to = null,
            string? species = null)
        {
            var result = new SightingResult();
            bool externalAvailable = false;
            bool localAvailable = false;

            try
            {
                var batch = await _cache.GetOrCreateAsync(
                    "external-tick-sightings",
                    async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromMinutes(5);

                        using var response = await _http.GetAsync(
                            "https://dev-task.elancoapps.com/data/tick-sightings");

                        response.EnsureSuccessStatusCode();

                        using var stream =
                            await response.Content.ReadAsStreamAsync();

                        using var document =
                            await JsonDocument.ParseAsync(stream);

                        if (document.RootElement.ValueKind != JsonValueKind.Array)
                        {
                            throw new JsonException("Expected an array.");
                        }

                        var loaded = new ExternalBatch();
                        var ids = new HashSet<string>();

                        foreach (var item in document.RootElement.EnumerateArray())
                        {
                            string? id = ReadText(item, "id");
                            string? location = ReadText(item, "location");
                            string? dateText = ReadText(item, "date");

                            if (string.IsNullOrWhiteSpace(id) ||
                                string.IsNullOrWhiteSpace(location) ||
                                !DateTime.TryParse(
                                    dateText,
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out var date) ||
                                date == default ||
                                date.Date > DateTime.UtcNow.Date)
                            {
                                loaded.Skipped++;
                                continue;
                            }

                            if (!ids.Add(id.Trim()))
                            {
                                loaded.Skipped++;
                                continue;
                            }

                            loaded.Records.Add(new SightingRecord
                            {
                                Id = "api-" + id.Trim(),
                                Date = date,
                                Location = location.Trim(),
                                Species = ReadText(item, "species")?.Trim()
                                    is { Length: > 0 } name ? name : "Unknown",
                                LatinName = ReadText(item, "latinName"),
                                Source = "Elanco educational API"
                            });
                        }

                        return loaded;
                    });

                result.Sightings.AddRange(batch!.Records);
                externalAvailable = true;

                if (batch.Skipped > 0)
                {
                    result.Warnings.Add(
                        $"{batch.Skipped} duplicate or invalid API records were excluded.");
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "External sightings unavailable.");
                result.Warnings.Add(
                    "External sightings are unavailable. Community reports are still requested.");
            }

            try
            {
                var query = _context.TickSightings.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(city))
                {
                    string cityName = city.Trim();
                    query = query.Where(item => item.City == cityName);
                }

                if (from.HasValue)
                {
                    var start = from.Value.Date;
                    query = query.Where(item => item.SightingDate >= start);
                }

                if (to.HasValue)
                {
                    var end = to.Value.Date;
                    query = query.Where(item => item.SightingDate.Date <= end);
                }

                if (!string.IsNullOrWhiteSpace(species))
                {
                    string speciesName = species.Trim();
                    query = query.Where(item => item.Species == speciesName);
                }

                var local = await query.Select(item => new SightingRecord
                {
                    Id = "local-" + item.TickSightingId,
                    Date = item.SightingDate,
                    Location = item.City,
                    Species = item.Species,
                    LatinName = item.LatinName,
                    Description = item.Description,
                    ImagePath = item.ImagePath,
                    Source = "Community report"
                }).ToListAsync();

                result.Sightings.AddRange(local);
                localAvailable = true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Community sightings unavailable.");
                result.Warnings.Add("Community reports are currently unavailable.");
            }

            if (!externalAvailable && !localAvailable)
            {
                throw new InvalidOperationException(
                    "Neither sightings source is available.");
            }

            result.Sightings = result.Sightings
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item.Location) &&
                    item.Date != default &&
                    item.Date.Date <= DateTime.UtcNow.Date)
                .Where(item => string.IsNullOrWhiteSpace(city) ||
                    item.Location.Trim().Equals(
                        city.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(item => !from.HasValue ||
                    item.Date.Date >= from.Value.Date)
                .Where(item => !to.HasValue ||
                    item.Date.Date <= to.Value.Date)
                .Where(item => string.IsNullOrWhiteSpace(species) ||
                    item.Species.Equals(
                        species.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Date)
                .ToList();

            return result;
        }

        private static string? ReadText(JsonElement item, string property)
        {
            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty(property, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }
    }
}