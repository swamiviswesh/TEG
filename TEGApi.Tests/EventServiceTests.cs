// TEGEventsAPI.Tests/EventServiceTests.cs
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using TEGApi;
using TEGApi.Model;
using Xunit;

namespace TEGApi.Tests;

public class EventServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<EventService>> _loggerMock;
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<EventService>>();
        _eventService = new EventService(
            _httpClientFactoryMock.Object,
            _cacheMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsCachedData_WhenCacheHit()
    {
        // Arrange
        var cachedData = new EventDataResponse
        {
            Events = new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Name = "Test Event",
                    StartDate = "2024-12-01T19:00:00Z",
                    VenueId = 1
                }
            },
            Venues = new List<Venue>
            {
                new Venue
                {
                    Id = 1,
                    Name = "Test Venue",
                    Location = "Sydney",
                    Capacity = 1000
                }
            }
        };

        object cacheValue = cachedData;
        _cacheMock
            .Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Event", result[0].Name);
        Assert.Equal(1, result[0].VenueId);
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetEventsAsync_FetchesFromSource_WhenCacheMiss()
    {
        // Arrange
        var jsonResponse = @"{
            ""events"": [
                {
                    ""id"": 1,
                    ""name"": ""Rock Concert"",
                    ""description"": ""Amazing rock show"",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 1
                }
            ],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Sydney Opera House"",
                    ""capacity"": 5000,
                    ""location"": ""Sydney, NSW""
                }
            ]
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        object? cacheValue = null;
        _cacheMock
            .Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        _cacheMock
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Rock Concert", result[0].Name);
        Assert.Equal(1, result[0].VenueId);
    }

    [Fact]
    public async Task GetVenuesAsync_ReturnsVenueList()
    {
        // Arrange
        var jsonResponse = @"{
            ""events"": [],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Venue A"",
                    ""capacity"": 1000,
                    ""location"": ""Sydney""
                },
                {
                    ""id"": 2,
                    ""name"": ""Venue B"",
                    ""capacity"": 2000,
                    ""location"": ""Melbourne""
                }
            ]
        }";

        SetupHttpMock(jsonResponse);

        // Act
        var result = await _eventService.GetVenuesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.Name == "Venue A");
        Assert.Contains(result, v => v.Name == "Venue B");
    }

    [Fact]
    public async Task GetEventsByVenueIdAsync_ReturnsFilteredEvents()
    {
        // Arrange
        var jsonResponse = @"{
            ""events"": [
                {
                    ""id"": 1,
                    ""name"": ""Event 1"",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 1
                },
                {
                    ""id"": 2,
                    ""name"": ""Event 2"",
                    ""startDate"": ""2024-12-02T19:00:00Z"",
                    ""venueId"": 2
                },
                {
                    ""id"": 3,
                    ""name"": ""Event 3"",
                    ""startDate"": ""2024-12-03T19:00:00Z"",
                    ""venueId"": 1
                }
            ],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Venue A"",
                    ""capacity"": 1000,
                    ""location"": ""Sydney""
                },
                {
                    ""id"": 2,
                    ""name"": ""Venue B"",
                    ""capacity"": 2000,
                    ""location"": ""Melbourne""
                }
            ]
        }";

        SetupHttpMock(jsonResponse);

        // Act
        var result = await _eventService.GetEventsByVenueIdAsync(1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(1, e.VenueId));
    }

    [Fact]
    public async Task GetEnrichedEventsAsync_JoinsEventsWithVenues()
    {
        // Arrange
        var jsonResponse = @"{
            ""events"": [
                {
                    ""id"": 1,
                    ""name"": ""Concert"",
                    ""description"": ""Great show"",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 1
                }
            ],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Sydney Opera House"",
                    ""capacity"": 5000,
                    ""location"": ""Sydney, NSW""
                }
            ]
        }";

        SetupHttpMock(jsonResponse);

        // Act
        var result = await _eventService.GetEnrichedEventsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Concert", result[0].Name);
        Assert.Equal("Sydney Opera House", result[0].VenueName);
        Assert.Equal("Sydney, NSW", result[0].VenueLocation);
        Assert.Equal(5000, result[0].VenueCapacity);
    }

    [Fact]
    public async Task GetEnrichedEventsAsync_HandlesOrphanedEvents()
    {
        // Arrange - Event with non-existent venueId
        var jsonResponse = @"{
            ""events"": [
                {
                    ""id"": 1,
                    ""name"": ""Orphaned Event"",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 999
                }
            ],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Test Venue"",
                    ""capacity"": 1000,
                    ""location"": ""Sydney""
                }
            ]
        }";

        SetupHttpMock(jsonResponse);

        // Act
        var result = await _eventService.GetEnrichedEventsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Unknown Venue", result[0].VenueName);
        Assert.Equal("Unknown Location", result[0].VenueLocation);
        Assert.Equal(0, result[0].VenueCapacity);
    }

    [Fact]
    public async Task ParseEventData_FiltersInvalidEvents()
    {
        // Arrange
        var jsonResponse = @"{
            ""events"": [
                {
                    ""id"": 1,
                    ""name"": ""Valid Event"",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 1
                },
                {
                    ""id"": 0,
                    ""name"": ""Invalid Event - No ID"",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 1
                },
                {
                    ""id"": 3,
                    ""name"": """",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 1
                },
                {
                    ""id"": 4,
                    ""name"": ""Invalid Event - No Date"",
                    ""startDate"": """",
                    ""venueId"": 1
                }
            ],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Test Venue"",
                    ""capacity"": 1000,
                    ""location"": ""Sydney""
                }
            ]
        }";

        SetupHttpMock(jsonResponse);

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Valid Event", result[0].Name);
    }

    [Fact]
    public async Task ParseEventData_FiltersInvalidVenues()
    {
        // Arrange
        var jsonResponse = @"{
            ""events"": [],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Valid Venue"",
                    ""capacity"": 1000,
                    ""location"": ""Sydney""
                },
                {
                    ""id"": 0,
                    ""name"": ""Invalid Venue - No ID"",
                    ""capacity"": 1000,
                    ""location"": ""Melbourne""
                },
                {
                    ""id"": 3,
                    ""name"": """",
                    ""capacity"": 1000,
                    ""location"": ""Brisbane""
                }
            ]
        }";

        SetupHttpMock(jsonResponse);

        // Act
        var result = await _eventService.GetVenuesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Valid Venue", result[0].Name);
    }

    [Fact]
    public async Task GetEnrichedEventsAsync_SortsEventsByStartDate()
    {
        // Arrange
        var jsonResponse = @"{
            ""events"": [
                {
                    ""id"": 1,
                    ""name"": ""Event C"",
                    ""startDate"": ""2024-12-03T19:00:00Z"",
                    ""venueId"": 1
                },
                {
                    ""id"": 2,
                    ""name"": ""Event A"",
                    ""startDate"": ""2024-12-01T19:00:00Z"",
                    ""venueId"": 1
                },
                {
                    ""id"": 3,
                    ""name"": ""Event B"",
                    ""startDate"": ""2024-12-02T19:00:00Z"",
                    ""venueId"": 1
                }
            ],
            ""venues"": [
                {
                    ""id"": 1,
                    ""name"": ""Test Venue"",
                    ""capacity"": 1000,
                    ""location"": ""Sydney""
                }
            ]
        }";

        SetupHttpMock(jsonResponse);

        // Act
        var result = await _eventService.GetEnrichedEventsAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Event A", result[0].Name);
        Assert.Equal("Event B", result[1].Name);
        Assert.Equal("Event C", result[2].Name);
    }

    private void SetupHttpMock(string jsonResponse)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        object? cacheValue = null;
        _cacheMock
            .Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        _cacheMock
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());
    }
}