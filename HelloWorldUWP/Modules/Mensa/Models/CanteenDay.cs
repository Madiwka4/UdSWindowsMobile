using System;

namespace HelloWorldUWP.Modules.Mensa.Models
{
    public class CanteenDay
    {
        public DateTime Date { get; set; }
        public string DateString { get; set; }
        public bool IsClosed { get; set; }

        public string DisplayText
        {
            get
            {
                var today = DateTime.Today;
                var prefix = "";
                if (Date.Date == today)
                {
                    prefix = "Heute, ";
                }
                else if (Date.Date == today.AddDays(1))
                {
                    prefix = "Morgen, ";
                }

                // e.g. "Heute, Mo 28.09" or "Di 29.09"
                return $"{prefix}{Date:ddd dd.MM}";
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
