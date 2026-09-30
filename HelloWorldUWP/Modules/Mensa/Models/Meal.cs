using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HelloWorldUWP.Modules.Mensa.Models
{
    public class Meal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal? StudentPrice { get; set; }
        public List<string> RawNotes { get; set; } = new List<string>();

        public string FormattedPrice
        {
            get
            {
                if (StudentPrice.HasValue && StudentPrice.Value > 0)
                {
                    return string.Format(new CultureInfo("de-DE"), "{0:C2}", StudentPrice.Value);
                }
                return "–";
            }
        }

        public bool HasPrice => StudentPrice.HasValue && StudentPrice.Value > 0;
        public Windows.UI.Xaml.Visibility PriceVisibility => HasPrice ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;

        public string Location { get; set; }
        public string TimeSlot { get; set; }
        public string SidesText { get; set; }
        public string AllergensText { get; set; }
        public List<string> Badges { get; set; } = new List<string>();

        public bool HasLocationOrTime => !string.IsNullOrWhiteSpace(Location) || !string.IsNullOrWhiteSpace(TimeSlot);
        public bool HasSides => !string.IsNullOrWhiteSpace(SidesText);
        public bool HasBadges => Badges != null && Badges.Count > 0;
        public bool HasAllergens => !string.IsNullOrWhiteSpace(AllergensText);

        public static Meal FromRaw(int id, string name, string category, decimal? studentPrice, List<string> notes)
        {
            var meal = new Meal
            {
                Id = id,
                Name = name,
                Category = category,
                StudentPrice = studentPrice,
                RawNotes = notes ?? new List<string>()
            };

            var sides = new List<string>();
            var allergens = new List<string>();
            var badges = new List<string>();

            // Inspect name for tags like "KlimaTeller", "Vegan:"
            if (name != null)
            {
                if (name.IndexOf("KlimaTeller", StringComparison.OrdinalIgnoreCase) >= 0)
                    badges.Add("KlimaTeller");
                if (name.IndexOf("Vegan", StringComparison.OrdinalIgnoreCase) >= 0 && !badges.Contains("Vegan"))
                    badges.Add("Vegan");
                if (name.IndexOf("mensaVital", StringComparison.OrdinalIgnoreCase) >= 0)
                    badges.Add("mensaVital");
            }

            foreach (var note in meal.RawNotes)
            {
                var trimmed = note.Trim();
                if (trimmed.StartsWith("Ort:", StringComparison.OrdinalIgnoreCase))
                {
                    meal.Location = trimmed.Substring(4).Trim();
                }
                else if (trimmed.StartsWith("verfügbar", StringComparison.OrdinalIgnoreCase))
                {
                    meal.TimeSlot = trimmed;
                }
                else if (trimmed.Equals("vegetarisch", StringComparison.OrdinalIgnoreCase))
                {
                    if (!badges.Contains("Vegetarisch")) badges.Add("Vegetarisch");
                }
                else if (trimmed.Equals("vegan", StringComparison.OrdinalIgnoreCase))
                {
                    if (!badges.Contains("Vegan")) badges.Add("Vegan");
                }
                else if (trimmed.IndexOf("Schweinefleisch", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!badges.Contains("Schwein")) badges.Add("Schwein");
                }
                else if (trimmed.IndexOf("Geflügel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!badges.Contains("Geflügel")) badges.Add("Geflügel");
                }
                else if (trimmed.IndexOf("Rind", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!badges.Contains("Rind")) badges.Add("Rind");
                }
                else if (trimmed.IndexOf("Fisch", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!badges.Contains("Fisch")) badges.Add("Fisch");
                }
                else if (trimmed.StartsWith("dazu", StringComparison.OrdinalIgnoreCase))
                {
                    sides.Add(trimmed);
                }
                else if (trimmed.StartsWith("enthält", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("mit", StringComparison.OrdinalIgnoreCase))
                {
                    allergens.Add(trimmed);
                }
                else
                {
                    // Generic note or badge
                    if (trimmed.Length < 25 && !trimmed.Contains(","))
                    {
                        if (!badges.Contains(trimmed)) badges.Add(trimmed);
                    }
                    else
                    {
                        sides.Add(trimmed);
                    }
                }
            }

            meal.SidesText = string.Join("\n", sides);
            meal.AllergensText = string.Join(", ", allergens);
            meal.Badges = badges;

            return meal;
        }
    }
}
