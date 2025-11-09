using System.Text.Json.Serialization;

namespace TEGApi.Model
{
    public record Venue
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("capacity")]
        public int Capacity { get; init; }

        [JsonPropertyName("location")]
        public string Location { get; init; } = string.Empty;
    }

}
