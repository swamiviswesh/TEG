using System.Text.Json.Serialization;

namespace TEGApi.Model
{
    public record EventDataResponse
    {
        [JsonPropertyName("events")]
        public List<Event> Events { get; init; } = new();

        [JsonPropertyName("venues")]
        public List<Venue> Venues { get; init; } = new();
    }

}
