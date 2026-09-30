using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using HelloWorldUWP.Modules.Mensa.Models;

namespace HelloWorldUWP.Modules.Mensa.Services
{
    public class OpenMensaService : IOpenMensaService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12)
        };

        private const string BaseUrl = "https://openmensa.org/api/v2";

        public Task<List<Canteen>> GetCanteensAsync()
        {
            var list = new List<Canteen>
            {
                new Canteen
                {
                    Id = 261,
                    Name = "Mensa Saarbrücken (Campus D4.1)",
                    ShortName = "Mensa Saarbrücken",
                    City = "Saarbrücken",
                    Address = "Campus D4.1, 66123 Saarbrücken"
                },
                new Canteen
                {
                    Id = 777,
                    Name = "Mensa Homburg (Gebäude 74)",
                    ShortName = "Mensa Homburg",
                    City = "Homburg",
                    Address = "Universitätsklinikum Geb. 74, 66424 Homburg"
                },
                new Canteen
                {
                    Id = 780,
                    Name = "Mensagarten Saarbrücken (A1.7)",
                    ShortName = "Mensagarten SB",
                    City = "Saarbrücken",
                    Address = "Campus Saarbrücken, Geb. A1.7"
                },
                new Canteen
                {
                    Id = 828,
                    Name = "Mensa HTW Saar Alt-Saarbrücken",
                    ShortName = "HTW Alt-Saarbrücken",
                    City = "Saarbrücken",
                    Address = "Goebenstraße 40, 66117 Saarbrücken"
                },
                new Canteen
                {
                    Id = 829,
                    Name = "Mensa HTW Saar Rotenbühl",
                    ShortName = "HTW Rotenbühl",
                    City = "Saarbrücken",
                    Address = "Waldhausweg 14, 66123 Saarbrücken"
                }
            };

            return Task.FromResult(list);
        }

        public async Task<List<CanteenDay>> GetOpenDaysAsync(int canteenId)
        {
            var days = new List<CanteenDay>();
            string cacheFileName = $"days_{canteenId}.json";
            string json = null;

            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/canteens/{canteenId}/days");
                if (response.IsSuccessStatusCode)
                {
                    json = await response.Content.ReadAsStringAsync();
                    await SaveCacheFileAsync(cacheFileName, json);
                }
            }
            catch
            {
                // Network unavailable, fallback to cache
                json = await LoadCacheFileAsync(cacheFileName);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                json = await LoadCacheFileAsync(cacheFileName);
            }

            if (!string.IsNullOrWhiteSpace(json) && JsonArray.TryParse(json, out var jsonArray))
            {
                foreach (var item in jsonArray)
                {
                    if (item.ValueType != JsonValueType.Object) continue;
                    var obj = item.GetObject();
                    var dateStr = obj.GetNamedString("date", "");
                    var isClosed = obj.GetNamedBoolean("closed", false);

                    if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        // Show open days from today onwards (or previous day if earlier)
                        if (!isClosed && dt.Date >= DateTime.Today.AddDays(-1))
                        {
                            days.Add(new CanteenDay
                            {
                                Date = dt,
                                DateString = dateStr,
                                IsClosed = isClosed
                            });
                        }
                    }
                }
            }

            // Fallback: If no days returned (e.g. offline with empty cache), supply current week workdays
            if (days.Count == 0)
            {
                var current = DateTime.Today;
                for (int i = 0; i < 7; i++)
                {
                    var d = current.AddDays(i);
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    {
                        days.Add(new CanteenDay
                        {
                            Date = d,
                            DateString = d.ToString("yyyy-MM-dd"),
                            IsClosed = false
                        });
                    }
                }
            }

            return days;
        }

        public async Task<MealsResult> GetMealsAsync(int canteenId, DateTime date, bool forceRefresh = false)
        {
            var result = new MealsResult();
            string dateStr = date.ToString("yyyy-MM-dd");
            string cacheFileName = $"meals_{canteenId}_{date:yyyyMMdd}.json";
            string json = null;

            if (!forceRefresh)
            {
                json = await LoadCacheFileAsync(cacheFileName);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    result.IsFromCache = true;
                    ParseMealsJson(json, result);
                    return result;
                }
            }

            // Try fetching from OpenMensa API
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/canteens/{canteenId}/days/{dateStr}/meals");
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    result.IsClosed = true;
                    return result;
                }

                if (response.IsSuccessStatusCode)
                {
                    json = await response.Content.ReadAsStringAsync();
                    await SaveCacheFileAsync(cacheFileName, json);
                    result.IsFromCache = false;
                    ParseMealsJson(json, result);
                    return result;
                }

                result.ErrorMessage = $"Server antwortete mit Status {(int)response.StatusCode}";
            }
            catch (Exception)
            {
                // Network error: try cache fallback
                json = await LoadCacheFileAsync(cacheFileName);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    result.IsFromCache = true;
                    ParseMealsJson(json, result);
                    return result;
                }

                result.ErrorMessage = "Keine Internetverbindung und keine Offline-Daten vorhanden.";
            }

            return result;
        }

        private void ParseMealsJson(string json, MealsResult result)
        {
            if (!JsonArray.TryParse(json, out var jsonArray)) return;

            foreach (var item in jsonArray)
            {
                if (item.ValueType != JsonValueType.Object) continue;
                var obj = item.GetObject();

                int id = (int)obj.GetNamedNumber("id", 0);
                string name = obj.GetNamedString("name", "");
                string category = obj.GetNamedString("category", "Tagesgericht");

                decimal? studentPrice = null;
                if (obj.ContainsKey("prices"))
                {
                    var pricesValue = obj.GetNamedValue("prices");
                    if (pricesValue.ValueType == JsonValueType.Object)
                    {
                        var pricesObj = pricesValue.GetObject();
                        if (pricesObj.ContainsKey("students") && pricesObj.GetNamedValue("students").ValueType == JsonValueType.Number)
                        {
                            studentPrice = (decimal)pricesObj.GetNamedNumber("students");
                        }
                    }
                }

                var notesList = new List<string>();
                if (obj.ContainsKey("notes"))
                {
                    var notesValue = obj.GetNamedValue("notes");
                    if (notesValue.ValueType == JsonValueType.Array)
                    {
                        foreach (var noteItem in notesValue.GetArray())
                        {
                            if (noteItem.ValueType == JsonValueType.String)
                            {
                                notesList.Add(noteItem.GetString());
                            }
                        }
                    }
                }

                var meal = Meal.FromRaw(id, name, category, studentPrice, notesList);
                result.Meals.Add(meal);
            }
        }

        private async Task SaveCacheFileAsync(string fileName, string content)
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, content);
            }
            catch
            {
                // Ignore caching errors
            }
        }

        private async Task<string> LoadCacheFileAsync(string fileName)
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var item = await folder.TryGetItemAsync(fileName);
                if (item is StorageFile file)
                {
                    return await FileIO.ReadTextAsync(file);
                }
            }
            catch
            {
                // Ignore cache read errors
            }
            return null;
        }
    }
}
