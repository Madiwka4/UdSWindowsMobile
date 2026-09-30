using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelloWorldUWP.Modules.Transport.Models;

namespace HelloWorldUWP.Modules.Transport.Services
{
    public interface ITransitService
    {
        TransitStop GetDefaultFromStop();
        TransitStop GetDefaultToStop();
        Task<List<TransitStop>> GetStopSuggestionsAsync(string query);
        Task<TransitStop> ResolveStopAsync(string query);
        Task<List<BusConnection>> GetBusConnectionsAsync(string fromStopId, string toStopId, DateTimeOffset? departureTime = null);
    }
}
