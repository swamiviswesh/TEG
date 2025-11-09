namespace TEGApi.Model
{
    // Enriched event with venue details for frontend convenience
    public record EnrichedEvent
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTime StartDate { get; init; }
        public int VenueId { get; init; }
        public string VenueName { get; init; } = string.Empty;
        public string VenueLocation { get; init; } = string.Empty;
        public int VenueCapacity { get; init; }
    }
}
