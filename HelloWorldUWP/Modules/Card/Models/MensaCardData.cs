using System;
using System.Globalization;

namespace HelloWorldUWP.Modules.Card.Models
{
    public class MensaCardData
    {
        public int BalanceInCents { get; set; }
        public decimal BalanceInEuros => BalanceInCents / 100.0m;
        public string FormattedBalance => $"{BalanceInEuros.ToString("0.00", new CultureInfo("de-DE"))} €";
        public string CardId { get; set; } = string.Empty;
        public DateTime ReadTimestamp { get; set; } = DateTime.Now;
        public string FormattedReadTime => ReadTimestamp.ToString("dd.MM.yyyy, HH:mm:ss");
        public string CardType { get; set; } = "UdS-Card (MIFARE DESFire EV1)";
        public string SystemVendor { get; set; } = "InterCard / Secanda";
        public bool IsSimulated { get; set; }
    }
}
