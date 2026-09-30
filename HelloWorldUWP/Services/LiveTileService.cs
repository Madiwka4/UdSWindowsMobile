using System;
using System.Collections.Generic;
using System.Net;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using HelloWorldUWP.Modules.Mensa.Models;

namespace HelloWorldUWP.Services
{
    public static class LiveTileService
    {
        /// <summary>
        /// Updates the Start Screen Live Tile with today's meals.
        /// Enables the Windows 10 notification queue so the tile cycles through meals.
        /// </summary>
        public static void UpdateLiveTile(string canteenName, List<Meal> meals, DateTime date)
        {
            try
            {
                var updater = TileUpdateManager.CreateTileUpdaterForApplication();
                updater.EnableNotificationQueue(true);
                updater.Clear();

                if (meals == null || meals.Count == 0)
                {
                    SetClosedTile(updater, canteenName, date);
                    return;
                }

                // Add up to 5 meals to the Live Tile cycling queue
                int count = 0;
                foreach (var meal in meals)
                {
                    if (count >= 5) break;

                    var tileXml = CreateMealTileXml(canteenName, meal, date);
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(tileXml);

                    var notification = new TileNotification(xmlDoc)
                    {
                        // Expire at the end of the day
                        ExpirationTime = date.Date.AddDays(1)
                    };

                    updater.Update(notification);
                    count++;
                }
            }
            catch
            {
                // Silently ignore tile update errors to never crash the app
            }
        }

        private static string CreateMealTileXml(string canteenName, Meal meal, DateTime date)
        {
            string category = WebUtility.HtmlEncode(meal.Category ?? "Mensa");
            string price = WebUtility.HtmlEncode(meal.FormattedPrice ?? "");
            string name = WebUtility.HtmlEncode(meal.Name ?? "");
            string location = WebUtility.HtmlEncode(meal.Location ?? "");
            string sides = WebUtility.HtmlEncode(meal.SidesText ?? "");

            string header = string.IsNullOrWhiteSpace(price) || price == "–" 
                ? category 
                : $"{category} • {price}";

            string locText = string.IsNullOrWhiteSpace(location) ? "" : $" ({location})";

            return $@"
<tile>
  <visual version=""3"">
    <binding template=""TileMedium"" branding=""nameAndLogo"" displayName=""Saarland Uni"">
      <text hint-style=""caption"" hint-weight=""bold"">{header}</text>
      <text hint-style=""captionSubtle"" hint-wrap=""true"" hint-maxLines=""3"">{name}</text>
      <text hint-style=""captionSubtle"">{location}</text>
    </binding>
    <binding template=""TileWide"" branding=""nameAndLogo"" displayName=""Saarland Uni"">
      <text hint-style=""body"" hint-weight=""bold"">{header}{locText}</text>
      <text hint-style=""caption"" hint-wrap=""true"" hint-maxLines=""2"">{name}</text>
      <text hint-style=""captionSubtle"" hint-wrap=""true"" hint-maxLines=""1"">{sides}</text>
    </binding>
  </visual>
</tile>";
        }

        private static void SetClosedTile(TileUpdater updater, string canteenName, DateTime date)
        {
            string safeCanteen = WebUtility.HtmlEncode(canteenName ?? "Mensa");
            string dateText = date.Date == DateTime.Today ? "Heute" : date.ToString("dd.MM.");

            string xml = $@"
<tile>
  <visual version=""3"">
    <binding template=""TileMedium"" branding=""nameAndLogo"" displayName=""Saarland Uni"">
      <text hint-style=""caption"" hint-weight=""bold"">{safeCanteen}</text>
      <text hint-style=""captionSubtle"">{dateText} geschlossen</text>
      <text hint-style=""captionSubtle"" hint-wrap=""true"">Keine Speisen verfügbar</text>
    </binding>
    <binding template=""TileWide"" branding=""nameAndLogo"" displayName=""Saarland Uni"">
      <text hint-style=""body"" hint-weight=""bold"">{safeCanteen} ({dateText})</text>
      <text hint-style=""caption"">Heute geschlossen</text>
      <text hint-style=""captionSubtle"">Öffnungszeiten und Menüs in der App einsehbar.</text>
    </binding>
  </visual>
</tile>";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            updater.Update(new TileNotification(xmlDoc)
            {
                ExpirationTime = DateTimeOffset.Now.AddDays(1)
            });
        }
    }
}
