import React, { useState, useEffect } from 'react';

const API_URL = 'https://localhost:7128/api/events/enriched';

export default function EventApp() {
  const [events, setEvents] = useState([]);
  const [venues, setVenues] = useState(new Map());
  const [selectedVenueId, setSelectedVenueId] = useState('all');
  const [selectedEvent, setSelectedEvent] = useState(null);
  const [loading, setLoading] = useState(true);
  const [currentDate, setCurrentDate] = useState(new Date());

  useEffect(() => {
    loadEvents();
  }, []);

  useEffect(() => {
    if (events.length > 0) {
      const firstEventDate = new Date(events[0].startDate);
      if (!isNaN(firstEventDate.getTime())) {
        setCurrentDate(new Date(firstEventDate.getFullYear(), firstEventDate.getMonth(), 1));
      }
    }
  }, [events]);

  const loadEvents = async () => {
    try {
      const response = await fetch(API_URL);
      const data = await response.json();
      
      const validEvents = Array.isArray(data) ? data.filter(e => 
        e && e.id && e.name && e.startDate && e.venueId
      ) : [];

      const venueMap = new Map();
      validEvents.forEach(e => {
        if (e.venueName && !venueMap.has(e.venueId)) {
          venueMap.set(e.venueId, e.venueName);
        }
      });

      setEvents(validEvents);
      setVenues(venueMap);
      setLoading(false);
    } catch (err) {
      console.error('Error:', err);
      setLoading(false);
    }
  };

  const filteredEvents = events.filter(e => 
    selectedVenueId === 'all' || e.venueId === selectedVenueId
  );

  const getCalendarDays = () => {
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay());
    
    const endDate = new Date(lastDay);
    endDate.setDate(endDate.getDate() + (6 - lastDay.getDay()));
    
    const days = [];
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
      const dayYear = d.getFullYear();
      const dayMonth = d.getMonth();
      const dayDate = d.getDate();
      
      const dayEvents = filteredEvents.filter(event => {
        const eventDate = new Date(event.startDate);
        return eventDate.getFullYear() === dayYear &&
               eventDate.getMonth() === dayMonth &&
               eventDate.getDate() === dayDate;
      });
      
      const currentDay = new Date(d);
      currentDay.setHours(0, 0, 0, 0);
      
      days.push({
        date: new Date(d),
        events: dayEvents,
        isCurrentMonth: d.getMonth() === month,
        isToday: currentDay.getTime() === today.getTime()
      });
    }
    
    return days;
  };

  const formatTime = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-AU', { hour: '2-digit', minute: '2-digit' });
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-AU', { 
      weekday: 'short', 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  };

  if (loading) {
    return (
      <div style={{minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: '#f5f5f5'}}>
        <p>Loading events...</p>
      </div>
    );
  }

  const calendarDays = getCalendarDays();
  const monthName = currentDate.toLocaleDateString('en-AU', { month: 'long', year: 'numeric' });

  return (
    <div style={{minHeight: '100vh', backgroundColor: '#f5f5f5', padding: '20px', fontFamily: 'Arial, sans-serif'}}>
      <div style={{maxWidth: '1200px', margin: '0 auto'}}>
        
        {/* Header (title, venue select) with legend on the right of venue */}
        <div style={{backgroundColor: 'white', padding: '20px', borderRadius: '8px', marginBottom: '20px', boxShadow: '0 2px 4px rgba(0,0,0,0.1)'}}>
          <div style={{display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '16px'}}>
            <div style={{minWidth: 0}}>
              <h1 style={{fontSize: '24px', margin: '0 0 12px 0'}}>TEG Events Calendar</h1>

              <div style={{display: 'flex', alignItems: 'center', gap: '10px'}}>
                <label style={{fontWeight: '600', fontSize: '14px'}}>What's on at </label>
                <select
                  value={selectedVenueId}
                  onChange={(e) => setSelectedVenueId(e.target.value === 'all' ? 'all' : Number(e.target.value))}
                  style={{flex: 1, padding: '10px', fontSize: '14px', border: '1px solid #ccc', borderRadius: '4px'}}
                >
                  <option value="all">All Venues</option>
                  {Array.from(venues.entries()).map(([id, name]) => (
                    <option key={id} value={id}>{name}</option>
                  ))}
                </select>
              </div>
            </div>

            {/* Legend placed to the right of venue select */}
            <div style={{display: 'flex', alignItems: 'center', gap: '8px', marginLeft: '16px'}}>
              <div style={{width: '16px', height: '16px', borderRadius: '50%', backgroundColor: '#90EE90', border: '2px solid #4CAF50'}} />
              <div style={{fontSize: '14px', color: '#333'}}>Event on!</div>
            </div>
          </div>
        </div>

        {/* Calendar */}
        <div style={{backgroundColor: 'white', borderRadius: '8px', boxShadow: '0 2px 4px rgba(0,0,0,0.1)', overflow: 'hidden'}}>
          
          {/* Month Navigation */}
          <div style={{backgroundColor: '#333', color: 'white', padding: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
            <button
              onClick={() => setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() - 1))}
              style={{backgroundColor: '#555', color: 'white', border: 'none', padding: '8px 12px', borderRadius: '4px', cursor: 'pointer', fontSize: '16px'}}
            >
              ◀
            </button>
            
            <h2 style={{fontSize: '20px', fontWeight: 'bold', margin: 0}}>{monthName}</h2>
            
            <button
              onClick={() => setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() + 1))}
              style={{backgroundColor: '#555', color: 'white', border: 'none', padding: '8px 12px', borderRadius: '4px', cursor: 'pointer', fontSize: '16px'}}
            >
              ▶
            </button>
          </div>

          {/* Calendar Grid */}
          <div style={{padding: '20px'}}>
            
            {/* Day Names */}
            <div style={{display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: '10px', marginBottom: '10px'}}>
              {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => (
                <div key={day} style={{textAlign: 'center', fontWeight: 'bold', padding: '10px', color: '#666'}}>
                  {day}
                </div>
              ))}
            </div>

            {/* Days Grid */}
            <div style={{display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: '10px'}}>
              {calendarDays.map((day, index) => {
                const isOtherMonth = !day.isCurrentMonth;
                const isToday = day.isToday;
                
                return (
                  <div
                    key={index}
                    style={{
                      minHeight: '100px',
                      border: isToday ? '2px solid #0066cc' : '1px solid #ddd',
                      borderRadius: '4px',
                      padding: '8px',
                      backgroundColor: isOtherMonth ? '#f9f9f9' : 'white'
                    }}
                  >
                    <div style={{
                      fontWeight: 'bold',
                      marginBottom: '8px',
                      color: isToday ? '#0066cc' : (isOtherMonth ? '#999' : '#333')
                    }}>
                      {day.date.getDate()}
                    </div>
                    
                    {/* ONLY CIRCLES - NO TEXT */}
                    <div style={{display: 'flex', flexWrap: 'wrap', gap: '4px'}}>
                      {day.events.map(event => (
                        <div
                          key={event.id}
                          onClick={() => setSelectedEvent(event)}
                          title={event.name}
                          style={{
                            width: '16px',
                            height: '16px',
                            borderRadius: '50%',
                            // light green circle for events
                            backgroundColor: '#90EE90',
                            border: '2px solid #4CAF50',
                            cursor: 'pointer',
                            flexShrink: 0
                          }}
                        />
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>

      {/* Event Detail Modal */}
      {selectedEvent && (
        <div 
          style={{position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: 'rgba(0,0,0,0.5)', display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px', zIndex: 1000}}
          onClick={() => setSelectedEvent(null)}
        >
          <div 
            style={{backgroundColor: 'white', borderRadius: '8px', maxWidth: '600px', width: '100%', maxHeight: '90vh', overflow: 'auto'}}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{backgroundColor: '#333', color: 'white', padding: '20px', borderRadius: '8px 8px 0 0'}}>
              <h2 style={{margin: 0, fontSize: '20px'}}>{selectedEvent.name}</h2>
            </div>
            
            <div style={{padding: '20px'}}>
              <div style={{marginBottom: '20px'}}>
                <div style={{fontWeight: 'bold', color: '#666', fontSize: '12px', textTransform: 'uppercase', marginBottom: '8px'}}>
                  Venue
                </div>
                <div style={{color: '#333', lineHeight: 1.6}}>
                  {selectedEvent.venueName || 'Unknown'}
                </div>
              </div>

              <div style={{marginBottom: '20px'}}>
                <div style={{fontWeight: 'bold', color: '#666', fontSize: '12px', textTransform: 'uppercase', marginBottom: '8px'}}>
                  Location
                </div>
                <div style={{color: '#333', lineHeight: 1.6}}>
                  {selectedEvent.venueLocation || 'Unknown'}
                </div>
              </div>

              <div style={{marginBottom: '20px'}}>
                <div style={{fontWeight: 'bold', color: '#666', fontSize: '12px', textTransform: 'uppercase', marginBottom: '8px'}}>
                  Capacity
                </div>
                <div style={{color: '#333', lineHeight: 1.6}}>
                  {selectedEvent.venueCapacity > 0 
                    ? selectedEvent.venueCapacity.toLocaleString() + ' patrons'
                    : 'Not specified'}
                </div>
              </div>

              <div style={{marginBottom: '20px'}}>
                <div style={{fontWeight: 'bold', color: '#666', fontSize: '12px', textTransform: 'uppercase', marginBottom: '8px'}}>
                  Date & Time
                </div>
                <div style={{color: '#333', lineHeight: 1.6}}>
                  {formatDate(selectedEvent.startDate)} at {formatTime(selectedEvent.startDate)}
                </div>
              </div>

              {selectedEvent.description && (
                <div style={{marginBottom: '20px'}}>
                  <div style={{fontWeight: 'bold', color: '#666', fontSize: '12px', textTransform: 'uppercase', marginBottom: '8px'}}>
                    Description
                  </div>
                  <div style={{color: '#333', lineHeight: 1.6}}>
                    {selectedEvent.description}
                  </div>
                </div>
              )}

              <button 
                onClick={() => setSelectedEvent(null)} 
                style={{width: '100%', padding: '12px', backgroundColor: '#333', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '14px'}}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}