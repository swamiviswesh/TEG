using System.Text.Json.Serialization;
namespace TEGApi.Model
{

    public record Event
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("startDate")]
        public string StartDate { get; init; } = string.Empty;

        [JsonPropertyName("venueId")]
        public int VenueId { get; init; }
    }
}