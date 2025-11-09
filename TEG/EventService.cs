using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.Json.Serialization;
using TEGApi.Model;

namespace TEGApi
{
    public class EventService : IEventService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EventService> _logger;
        private const string CacheKey = "event_data";
        private const string SourceUrl = "https://teg-coding-challenge.s3.ap-southeast-2.amazonaws.com/events/event-data.json";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        public EventService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<EventService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            var data = await GetEventDataAsync();
            return data.Events;
        }

        public async Task<List<Venue>> GetVenuesAsync()
        {
            var data = await GetEventDataAsync();
            return data.Venues;
        }

        public async Task<List<Event>> GetEventsByVenueIdAsync(int venueId)
        {
            var data = await GetEventDataAsync();
            return data.Events
                .Where(e => e.VenueId == venueId)
                .ToList();
        }

        public async Task<List<EnrichedEvent>> GetEnrichedEventsAsync()
        {
            var data = await GetEventDataAsync();

            // Create a lookup dictionary for venues
            var venuesDict = data.Venues.ToDictionary(v => v.Id);

            // Join events with venue information
            var enrichedEvents = data.Events
                .Select(e => new EnrichedEvent
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartDate = ParseStartDate(e.StartDate),
                    VenueId = e.VenueId,
                    VenueName = venuesDict.TryGetValue(e.VenueId, out var venue) ? venue.Name : "Unknown Venue",
                    VenueLocation = venuesDict.TryGetValue(e.VenueId, out var v) ? v.Location : "Unknown Location",
                    VenueCapacity = venuesDict.TryGetValue(e.VenueId, out var ven) ? ven.Capacity : 0
                })
                .OrderBy(e => e.StartDate)
                .ToList();

            return enrichedEvents;
        }

        private async Task<EventDataResponse> GetEventDataAsync()
        {
            // Try cache first
            if (_cache.TryGetValue(CacheKey, out EventDataResponse? cachedData) && cachedData != null)
            {
                _logger.LogInformation("Returning cached event data");
                return cachedData;
            }

            // Fetch from source with retry logic
            var data = await FetchEventDataWithRetryAsync();

            // Cache the results
            if (data.Events.Any() || data.Venues.Any())
            {
                _cache.Set(CacheKey, data, CacheDuration);
            }

            return data;
        }

        private async Task<EventDataResponse> FetchEventDataWithRetryAsync()
        {
            const int maxRetries = 3;
            var retryDelays = new[] {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5)
        };

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Fetching event data from source (attempt {Attempt})", attempt + 1);

                    var client = _httpClientFactory.CreateClient();
                    client.Timeout = RequestTimeout;

                    var response = await client.GetAsync(SourceUrl);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();

                    // Parse and validate
                    var data = ParseEventData(content);

                    _logger.LogInformation("Successfully fetched {EventCount} events and {VenueCount} venues",
                        data.Events.Count, data.Venues.Count);
                    return data;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed to fetch event data", attempt + 1);

                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay(retryDelays[attempt]);
                    }
                    else
                    {
                        // Last attempt failed, check if we have stale cache
                        if (_cache.TryGetValue(CacheKey, out EventDataResponse? staleData) && staleData != null)
                        {
                            _logger.LogWarning("Returning stale cached data due to source failure");
                            return staleData;
                        }

                        _logger.LogError("All retry attempts failed and no cache available");
                        throw new InvalidOperationException("Unable to fetch event data from source", ex);
                    }
                }
            }

            return new EventDataResponse();
        }

        private EventDataResponse ParseEventData(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var data = JsonSerializer.Deserialize<EventDataResponse>(json, options);

                if (data == null)
                {
                    _logger.LogWarning("Deserialized data is null");
                    return new EventDataResponse();
                }

                return ValidateAndCleanData(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing event data JSON");
                throw;
            }
        }

        private EventDataResponse ValidateAndCleanData(EventDataResponse data)
        {
            // Filter out invalid events (defensive coding)
            var validEvents = data.Events
                .Where(e => e.Id > 0
                         && !string.IsNullOrWhiteSpace(e.Name)
                         && !string.IsNullOrWhiteSpace(e.StartDate)
                         && e.VenueId > 0)
                .ToList();

            // Filter out invalid venues
            var validVenues = data.Venues
                .Where(v => v.Id > 0
                         && !string.IsNullOrWhiteSpace(v.Name))
                .ToList();

            if (validEvents.Count < data.Events.Count)
            {
                _logger.LogWarning("Filtered out {Count} invalid events", data.Events.Count - validEvents.Count);
            }

            if (validVenues.Count < data.Venues.Count)
            {
                _logger.LogWarning("Filtered out {Count} invalid venues", data.Venues.Count - validVenues.Count);
            }

            return new EventDataResponse
            {
                Events = validEvents,
                Venues = validVenues
            };
        }

        private DateTime ParseStartDate(string startDate)
        {
            try
            {
                return DateTime.Parse(startDate);
            }
            catch
            {
                _logger.LogWarning("Failed to parse start date: {StartDate}", startDate);
                return DateTime.MinValue;
            }
        }
    }
}
