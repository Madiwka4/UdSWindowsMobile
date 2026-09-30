using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Data.Json;
using HelloWorldUWP.Modules.Transport.Models;

namespace HelloWorldUWP.Modules.Transport.Services
{
    public class TransitousTransitService : ITransitService
    {
        private const string BaseApiUrl = "https://api.transitous.org/api/v1";
        private const string UserAgent = "SaarlandUniApp/1.0 (Windows 10 Mobile; support@uni-saarland.de)";

        private readonly HttpClient _httpClient;

        // Default stops requested by user
        private readonly TransitStop _defaultFromStop = new TransitStop
        {
            Id = "de-DELFI_de:10041:10612::1061201",
            Name = "Haus der Zukunft, Saarbrücken",
            DisplayName = "Haus der Zukunft",
            Lat = 49.238647,
            Lon = 6.998139,
            Modes = new List<string> { "BUS" }
        };

        private readonly TransitStop _defaultToStop = new TransitStop
        {
            Id = "de-DELFI_de:10041:10905::1090501",
            Name = "Universität Mensa, Saarbrücken",
            DisplayName = "University Mensa",
            Lat = 49.256363,
            Lon = 7.044069,
            Modes = new List<string> { "BUS" }
        };

        // Popular Saarbrücken campus & central transit stops for instant local matching
        private readonly List<TransitStop> _knownStops = new List<TransitStop>
        {
            // Campus & University
            new TransitStop { Id = "de-DELFI_de:10041:10612::1061201", Name = "Haus der Zukunft, Saarbrücken", DisplayName = "Haus der Zukunft", Lat = 49.238647, Lon = 6.998139 },
            new TransitStop { Id = "de-DELFI_de:10041:10905::1090501", Name = "Universität Mensa, Saarbrücken", DisplayName = "University Mensa", Lat = 49.256363, Lon = 7.044069 },
            new TransitStop { Id = "de-DELFI_de:10041:10904::1090402", Name = "Universität Campus, Saarbrücken", DisplayName = "University Campus", Lat = 49.25473, Lon = 7.041082 },
            new TransitStop { Id = "de-DELFI_de:10041:10909::1090906", Name = "Universität Busterminal, Saarbrücken", DisplayName = "University Busterminal", Lat = 49.25773, Lon = 7.047561 },
            new TransitStop { Id = "de-DELFI_de:10041:10903::1090301", Name = "Universität Botanischer Garten, Saarbrücken", DisplayName = "Botanischer Garten", Lat = 49.254734, Lon = 7.037564 },
            new TransitStop { Id = "de-DELFI_de:10041:10902::1090201", Name = "Sportcampus, Saarbrücken", DisplayName = "Sportcampus", Lat = 49.25223, Lon = 7.03234 },
            new TransitStop { Id = "de-DELFI_de:10041:10906::1090606", Name = "Stuhlsatzenhaus, Saarbrücken", DisplayName = "Stuhlsatzenhaus", Lat = 49.2591, Lon = 7.0515 },
            new TransitStop { Id = "de-DELFI_de:10041:10803::1080301", Name = "Waldhaus, Saarbrücken", DisplayName = "Waldhaus", Lat = 49.24621, Lon = 7.02133 },
            new TransitStop { Id = "de-DELFI_de:10041:10901::1090101", Name = "Wildpark, Saarbrücken", DisplayName = "Wildpark", Lat = 49.24891, Lon = 7.02652 },
            new TransitStop { Id = "de-DELFI_de:10041:10802::1080201", Name = "Prinzenweiher/DJH, Saarbrücken", DisplayName = "Prinzenweiher (Youth Hostel)", Lat = 49.24337, Lon = 7.01189 },
            new TransitStop { Id = "de-DELFI_de:10041:10801::1080101", Name = "An der Trift, Saarbrücken", DisplayName = "An der Trift", Lat = 49.24183, Lon = 7.00701 },
            new TransitStop { Id = "de-DELFI_de:10041:10613::1061301", Name = "Brauerstr./Fernbusbahnhof, Saarbrücken", DisplayName = "Brauerstr. (Fernbusbahnhof)", Lat = 49.24037, Lon = 7.00058 },

            // City Center / St. Johann / Train Station
            new TransitStop { Id = "de-DELFI_de:10041:8000323_G", Name = "Hauptbahnhof, Saarbrücken", DisplayName = "Saarbrücken Main Station (Hbf)", Lat = 49.2410, Lon = 6.9904 },
            new TransitStop { Id = "de-DELFI_de:10041:10619::1061906", Name = "Johanneskirche, Saarbrücken", DisplayName = "Johanneskirche", Lat = 49.23715, Lon = 6.99678 },
            new TransitStop { Id = "de-DELFI_de:10041:10601", Name = "Rathaus, Saarbrücken", DisplayName = "Rathaus (City Hall)", Lat = 49.2355, Lon = 6.9950 },
            new TransitStop { Id = "de-DELFI_de:10041:10603::1060316", Name = "Trierer Str., Saarbrücken", DisplayName = "Trierer Straße", Lat = 49.2392, Lon = 6.9885 },
            new TransitStop { Id = "de-DELFI_de:10041:10604::1060411", Name = "Kaiserstr., Saarbrücken", DisplayName = "Kaiserstraße", Lat = 49.2366, Lon = 6.9922 },
            new TransitStop { Id = "de-DELFI_de:10041:10640::1064001", Name = "Landwehrplatz, Saarbrücken", DisplayName = "Landwehrplatz", Lat = 49.2325, Lon = 7.0012 },
            new TransitStop { Id = "de-DELFI_de:10041:10642::1064201", Name = "Rabbiner-Rülf-Platz, Saarbrücken", DisplayName = "Rabbiner-Rülf-Platz", Lat = 49.2341, Lon = 6.9935 },
            new TransitStop { Id = "de-DELFI_de:10041:10644::1064401", Name = "Schlossplatz, Saarbrücken", DisplayName = "Schlossplatz (Alt-Saarbrücken)", Lat = 49.2302, Lon = 6.9928 },
            new TransitStop { Id = "de-DELFI_de:10041:10646::1064601", Name = "Uhlandstr., Saarbrücken", DisplayName = "Uhlandstraße", Lat = 49.2348, Lon = 7.0055 },
            new TransitStop { Id = "de-DELFI_de:10041:10648::1064801", Name = "Ilseplatz, Saarbrücken", DisplayName = "Ilseplatz", Lat = 49.2372, Lon = 7.0089 },

            // HTW & Tech Campuses
            new TransitStop { Id = "de-DELFI_de:10041:10304::1030401", Name = "HTW / Goebenstraße, Saarbrücken", DisplayName = "HTW Goebenstraße", Lat = 49.2312, Lon = 6.9830 },
            new TransitStop { Id = "de-DELFI_de:10041:10049::1004901", Name = "HTW/SWS, Saarbrücken", DisplayName = "HTW / SWS", Lat = 49.2339, Lon = 6.9779 },
            new TransitStop { Id = "de-DELFI_de:10041:10302::1030201", Name = "Rotenbühl, Saarbrücken", DisplayName = "Rotenbühl (HTW)", Lat = 49.2458, Lon = 7.0185 },

            // Dudweiler
            new TransitStop { Id = "de-DELFI_de:10041:12602::1260204", Name = "Dudoplatz, Dudweiler Saarbrücken", DisplayName = "Dudoplatz (Dudweiler)", Lat = 49.27667, Lon = 7.03346 },
            new TransitStop { Id = "de-DELFI_de:10041:12607::1260706", Name = "Markt, Dudweiler Saarbrücken", DisplayName = "Markt (Dudweiler)", Lat = 49.2755, Lon = 7.0357 },
            new TransitStop { Id = "de-DELFI_de:10041:12601::1260106", Name = "Bürgerhaus, Dudweiler Saarbrücken", DisplayName = "Bürgerhaus (Dudweiler)", Lat = 49.2738, Lon = 7.0402 },
            new TransitStop { Id = "de-DELFI_de:10041:12622::1262208", Name = "Guckelsberg, Dudweiler Saarbrücken", DisplayName = "Guckelsberg (Dudweiler)", Lat = 49.2656, Lon = 7.0496 },

            // Surrounding Key Hubs
            new TransitStop { Id = "de-DELFI_de:10041:8005241_G", Name = "Saarbrücken Ost", DisplayName = "Saarbrücken Ost (Station)", Lat = 49.2304, Lon = 7.0187 },
            new TransitStop { Id = "de-DELFI_de:10041:8005243_G", Name = "Saarbrücken-Burbach", DisplayName = "Saarbrücken-Burbach (Station)", Lat = 49.2420, Lon = 6.9565 },
            new TransitStop { Id = "de-DELFI_de:10041:10812::1081201", Name = "Bauhaus, Saarbrücken", DisplayName = "Bauhaus (Saarbrücken)", Lat = 49.2452, Lon = 7.0055 },
            new TransitStop { Id = "de-DELFI_de:10041:10702::1070201", Name = "Römerkastell, Saarbrücken", DisplayName = "Römerkastell", Lat = 49.2272, Lon = 7.0165 }
        };

        public TransitousTransitService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public TransitStop GetDefaultFromStop() => _defaultFromStop;

        public TransitStop GetDefaultToStop() => _defaultToStop;

        public async Task<List<TransitStop>> GetStopSuggestionsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return _knownStops.Take(6).ToList();
            }

            var cleanQuery = query.Trim().ToLowerInvariant();
            var results = new List<TransitStop>();

            // 1. Instant local matching against curated Saarbrücken stops
            foreach (var stop in _knownStops)
            {
                if (stop.DisplayName.ToLowerInvariant().Contains(cleanQuery) ||
                    stop.Name.ToLowerInvariant().Contains(cleanQuery))
                {
                    results.Add(stop);
                }
            }

            // 2. Query Transitous geocoding API, strictly scoped and filtered to Saarbrücken
            if (cleanQuery.Length >= 2)
            {
                try
                {
                    // Bias query explicitly to Saarbrücken
                    string geocodeSearch = cleanQuery.Contains("saarbr") ? query : $"{query} Saarbrücken";
                    var encoded = Uri.EscapeDataString(geocodeSearch);
                    var url = $"{BaseApiUrl}/geocode?text={encoded}&lat=49.2386&lon=6.9981";
                    var json = await _httpClient.GetStringAsync(url);

                    if (JsonArray.TryParse(json, out JsonArray array))
                    {
                        foreach (var val in array)
                        {
                            if (val.ValueType != JsonValueType.Object) continue;
                            var obj = val.GetObject();

                            string type = obj.ContainsKey("type") ? obj.GetNamedString("type") : "";
                            string name = obj.ContainsKey("name") ? obj.GetNamedString("name") : "";
                            string id = obj.ContainsKey("id") ? obj.GetNamedString("id") : "";

                            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) continue;

                            // Filter out non-transit items (e.g. shops, parking, restaurants)
                            if (!string.Equals(type, "STOP", StringComparison.OrdinalIgnoreCase) &&
                                !id.StartsWith("de-DELFI_de:10", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            double lat = obj.ContainsKey("lat") ? obj.GetNamedNumber("lat") : 0.0;
                            double lon = obj.ContainsKey("lon") ? obj.GetNamedNumber("lon") : 0.0;

                            // Strictly verify location belongs to Saarbrücken region
                            if (!IsInSaarbrueckenRegion(lat, lon, id, name))
                            {
                                continue;
                            }

                            if (results.Any(r => r.Id == id)) continue;

                            results.Add(new TransitStop
                            {
                                Id = id,
                                Name = name,
                                DisplayName = CleanDisplayName(name),
                                Lat = lat,
                                Lon = lon
                            });

                            if (results.Count >= 8) break;
                        }
                    }
                }
                catch
                {
                    // Fall back to local matches on network error
                }
            }

            return results.Count > 0 ? results : _knownStops.Take(6).ToList();
        }

        private bool IsInSaarbrueckenRegion(double lat, double lon, string id, string name)
        {
            // 1. DELFI ID check: 10041 is Regionalverband Saarbrücken (10 = Saarland, 10041 = Regionalverband Saarbrücken)
            if (!string.IsNullOrEmpty(id) && id.StartsWith("de-DELFI_de:10041", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 2. Name check: Must explicitly reference Saarbrücken or Dudweiler
            if (!string.IsNullOrEmpty(name))
            {
                string lower = name.ToLowerInvariant();
                if (lower.Contains("saarbr") || lower.Contains("dudweiler") || lower.Contains("st. johann") || lower.Contains("malstatt") || lower.Contains("burbach") || lower.Contains("rotenb"))
                {
                    return true;
                }
            }

            // 3. Geographic bounding box strictly around Saarbrücken / Regionalverband (lat: 49.15 to 49.35, lon: 6.88 to 7.15)
            if (lat >= 49.15 && lat <= 49.35 && lon >= 6.88 && lon <= 7.15)
            {
                return true;
            }

            return false;
        }

        public async Task<TransitStop> ResolveStopAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return _defaultFromStop;

            var suggestions = await GetStopSuggestionsAsync(query);
            return suggestions.FirstOrDefault() ?? _defaultFromStop;
        }

        public async Task<List<BusConnection>> GetBusConnectionsAsync(string fromStopId, string toStopId, DateTimeOffset? departureTime = null)
        {
            var connections = new List<BusConnection>();

            try
            {
                var timeParam = departureTime.HasValue
                    ? $"&time={Uri.EscapeDataString(departureTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))}"
                    : string.Empty;

                var url = $"{BaseApiUrl}/plan?fromPlace={Uri.EscapeDataString(fromStopId)}&toPlace={Uri.EscapeDataString(toStopId)}{timeParam}";
                var json = await _httpClient.GetStringAsync(url);

                if (!JsonObject.TryParse(json, out JsonObject root))
                {
                    return connections;
                }

                if (!root.ContainsKey("itineraries") || root.GetNamedValue("itineraries").ValueType != JsonValueType.Array)
                {
                    return connections;
                }

                var itinerariesArray = root.GetNamedArray("itineraries");
                var now = DateTimeOffset.Now;

                foreach (var item in itinerariesArray)
                {
                    if (item.ValueType != JsonValueType.Object) continue;
                    var itinObj = item.GetObject();

                    var connection = ParseItinerary(itinObj);
                    if (connection != null)
                    {
                        // Calculate initial countdown and visual progress
                        connection.UpdateProgress(now);
                        connections.Add(connection);
                    }
                }
            }
            catch
            {
                // Return whatever connections were parsed or empty list
            }

            return connections.OrderBy(c => c.RealtimeDeparture).ToList();
        }

        private BusConnection ParseItinerary(JsonObject itinObj)
        {
            try
            {
                if (!itinObj.ContainsKey("legs") || itinObj.GetNamedValue("legs").ValueType != JsonValueType.Array)
                {
                    return null;
                }

                var legsArray = itinObj.GetNamedArray("legs");
                if (legsArray.Count == 0) return null;

                int transfers = itinObj.ContainsKey("transfers") ? (int)itinObj.GetNamedNumber("transfers") : 0;

                // Identify transit legs (BUS, TRAM, etc.)
                var transitLegs = new List<JsonObject>();
                foreach (var legVal in legsArray)
                {
                    if (legVal.ValueType != JsonValueType.Object) continue;
                    var legObj = legVal.GetObject();
                    string mode = legObj.ContainsKey("mode") ? legObj.GetNamedString("mode") : "";
                    if (mode == "BUS" || mode == "TRAM" || mode == "TRANSIT")
                    {
                        transitLegs.Add(legObj);
                    }
                }

                if (transitLegs.Count == 0 && legsArray.Count > 0)
                {
                    // Fall back to first leg if only walk
                    transitLegs.Add(legsArray[0].GetObject());
                }

                var primaryLeg = transitLegs[0];

                // Overall times from itinerary or legs
                string startTimeStr = itinObj.ContainsKey("startTime") ? itinObj.GetNamedString("startTime") : "";
                string endTimeStr = itinObj.ContainsKey("endTime") ? itinObj.GetNamedString("endTime") : "";

                DateTimeOffset depTime = DateTimeOffset.TryParse(startTimeStr, out var parsedDep) ? parsedDep : DateTimeOffset.Now;
                DateTimeOffset arrTime = DateTimeOffset.TryParse(endTimeStr, out var parsedArr) ? parsedArr : depTime.AddMinutes(15);

                // Scheduled times
                DateTimeOffset schedDep = depTime;
                DateTimeOffset schedArr = arrTime;
                if (primaryLeg.ContainsKey("scheduledStartTime"))
                {
                    var s = primaryLeg.GetNamedString("scheduledStartTime");
                    if (DateTimeOffset.TryParse(s, out var psDep)) schedDep = psDep;
                }
                if (primaryLeg.ContainsKey("scheduledEndTime"))
                {
                    var s = primaryLeg.GetNamedString("scheduledEndTime");
                    if (DateTimeOffset.TryParse(s, out var psArr)) schedArr = psArr;
                }

                // Line numbers / Display name (e.g. "101" or "102 -> 101")
                var lineBadges = new List<string>();
                foreach (var tLeg in transitLegs)
                {
                    string name = tLeg.ContainsKey("displayName") ? tLeg.GetNamedString("displayName") : "";
                    if (string.IsNullOrWhiteSpace(name) && tLeg.ContainsKey("routeShortName"))
                    {
                        name = tLeg.GetNamedString("routeShortName");
                    }
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        lineBadges.Add(name);
                    }
                }

                string lineBadge = lineBadges.Count > 0 ? string.Join(" → ", lineBadges) : "Bus";

                // Headsign & Agency
                string headsign = primaryLeg.ContainsKey("headsign") ? primaryLeg.GetNamedString("headsign") : "";
                string agency = primaryLeg.ContainsKey("agencyName") ? primaryLeg.GetNamedString("agencyName") : "Saarbahn";

                // Route color
                string routeColor = "#E30613"; // Saarbahn default red
                if (primaryLeg.ContainsKey("routeColor"))
                {
                    string c = primaryLeg.GetNamedString("routeColor");
                    if (!string.IsNullOrWhiteSpace(c))
                    {
                        routeColor = c.StartsWith("#") ? c : $"#{c}";
                    }
                }

                string routeTextColor = "#FFFFFF";
                if (primaryLeg.ContainsKey("routeTextColor"))
                {
                    string tc = primaryLeg.GetNamedString("routeTextColor");
                    if (!string.IsNullOrWhiteSpace(tc))
                    {
                        routeTextColor = tc.StartsWith("#") ? tc : $"#{tc}";
                    }
                }

                // Real-time flag
                bool isRealTime = primaryLeg.ContainsKey("realTime") && primaryLeg.GetNamedBoolean("realTime");

                // Stop names
                string fromName = GetStopName(primaryLeg, "from");
                string toName = GetStopName(transitLegs.Last(), "to");

                // Intermediate stops
                var intermediateStops = new List<string>();
                if (primaryLeg.ContainsKey("intermediateStops"))
                {
                    var stopsVal = primaryLeg.GetNamedValue("intermediateStops");
                    if (stopsVal.ValueType == JsonValueType.Array)
                    {
                        foreach (var stopItem in stopsVal.GetArray())
                        {
                            if (stopItem.ValueType == JsonValueType.Object)
                            {
                                var stopObj = stopItem.GetObject();
                                if (stopObj.ContainsKey("name"))
                                {
                                    intermediateStops.Add(CleanDisplayName(stopObj.GetNamedString("name")));
                                }
                            }
                        }
                    }
                }

                string stopsSummary = intermediateStops.Count > 0
                    ? $"{intermediateStops.Count} stops: " + string.Join(", ", intermediateStops.Take(3)) + (intermediateStops.Count > 3 ? "..." : "")
                    : "Direct route";

                return new BusConnection
                {
                    LineNumber = lineBadge,
                    Headsign = CleanDisplayName(headsign),
                    AgencyName = agency,
                    RouteColor = routeColor,
                    RouteTextColor = routeTextColor,
                    FromStopName = CleanDisplayName(fromName),
                    ToStopName = CleanDisplayName(toName),
                    RealtimeDeparture = depTime,
                    RealtimeArrival = arrTime,
                    ScheduledDeparture = schedDep,
                    ScheduledArrival = schedArr,
                    IsRealTime = isRealTime,
                    Transfers = transfers,
                    IntermediateStops = intermediateStops,
                    IntermediateStopsSummary = stopsSummary
                };
            }
            catch
            {
                return null;
            }
        }

        private string GetStopName(JsonObject legObj, string key)
        {
            if (legObj.ContainsKey(key))
            {
                var val = legObj.GetNamedValue(key);
                if (val.ValueType == JsonValueType.Object)
                {
                    var obj = val.GetObject();
                    if (obj.ContainsKey("name")) return obj.GetNamedString("name");
                }
            }
            return string.Empty;
        }

        private string CleanDisplayName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;
            // Clean up suffixes like ", Saarbrücken" or ", Dudweiler Saarbrücken" for clean mobile display
            return rawName
                .Replace(", Saarbrücken", "")
                .Replace(", Dudweiler", "")
                .Replace("Saarbrücken", "")
                .Trim(',', ' ');
        }
    }
}
