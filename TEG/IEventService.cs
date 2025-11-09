using TEGApi.Model;

namespace TEGApi
{
    public interface IEventService
    {
        Task<List<Event>> GetEventsAsync();
        Task<List<Venue>> GetVenuesAsync();
        Task<List<Event>> GetEventsByVenueIdAsync(int venueId);
        Task<List<EnrichedEvent>> GetEnrichedEventsAsync();
    }
}
