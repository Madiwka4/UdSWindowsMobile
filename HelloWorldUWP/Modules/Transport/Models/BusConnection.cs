using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HelloWorldUWP.Modules.Transport.Models
{
    public enum BusTransitState
    {
        Upcoming,
        BoardingSoon,
        InTransit,
        Completed
    }

    public class BusConnection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string ItineraryId { get; set; } = Guid.NewGuid().ToString();
        public string LineNumber { get; set; } = "Bus";
        public string Headsign { get; set; } = string.Empty;
        public string AgencyName { get; set; } = "Saarbahn";
        public string RouteColor { get; set; } = "#E30613";
        public string RouteTextColor { get; set; } = "#FFFFFF";

        public string FromStopName { get; set; } = string.Empty;
        public string ToStopName { get; set; } = string.Empty;

        public DateTimeOffset ScheduledDeparture { get; set; }
        public DateTimeOffset ScheduledArrival { get; set; }
        public DateTimeOffset RealtimeDeparture { get; set; }
        public DateTimeOffset RealtimeArrival { get; set; }
        public bool IsRealTime { get; set; }

        public int Transfers { get; set; }
        public bool IsDirect => Transfers == 0;
        public string TransfersText => IsDirect ? "Direct" : $"{Transfers} transfer";

        public int DurationMinutes => Math.Max(1, (int)Math.Round((RealtimeArrival - RealtimeDeparture).TotalMinutes));
        public string DurationText => $"{DurationMinutes} min";

        public string DepartureTimeFormatted => RealtimeDeparture.ToLocalTime().ToString("HH:mm");
        public string ArrivalTimeFormatted => RealtimeArrival.ToLocalTime().ToString("HH:mm");

        public List<string> IntermediateStops { get; set; } = new List<string>();
        public int IntermediateStopsCount => IntermediateStops?.Count ?? 0;
        public string IntermediateStopsSummary { get; set; } = string.Empty;

        // Delay handling
        public int DelayMinutes
        {
            get
            {
                var delay = (RealtimeDeparture - ScheduledDeparture).TotalMinutes;
                return (int)Math.Round(delay);
            }
        }

        public string DelayText
        {
            get
            {
                if (!IsRealTime) return "Scheduled";
                if (DelayMinutes <= 0) return "On time";
                return $"+{DelayMinutes} min late";
            }
        }

        public string DelayColor
        {
            get
            {
                if (!IsRealTime) return "#888888";
                if (DelayMinutes <= 0) return "#107C41"; // Green
                if (DelayMinutes <= 3) return "#FF8C00"; // Orange
                return "#E81123"; // Red
            }
        }

        // Live Timer-Driven Properties
        private string _countdownText = string.Empty;
        public string CountdownText
        {
            get => _countdownText;
            set => SetProperty(ref _countdownText, value);
        }

        private string _statusBadge = "UPCOMING";
        public string StatusBadge
        {
            get => _statusBadge;
            set => SetProperty(ref _statusBadge, value);
        }

        private string _statusColor = "#0078D7";
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        private double _progressPercent = 0.0;
        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        private string _progressLabel = string.Empty;
        public string ProgressLabel
        {
            get => _progressLabel;
            set => SetProperty(ref _progressLabel, value);
        }

        private BusTransitState _transitState = BusTransitState.Upcoming;
        public BusTransitState TransitState
        {
            get => _transitState;
            set => SetProperty(ref _transitState, value);
        }

        /// <summary>
        /// Updates the real-time countdown, transit state, and visual progress based on current system time.
        /// </summary>
        public void UpdateProgress(DateTimeOffset now)
        {
            var timeUntilDeparture = RealtimeDeparture - now;
            var minutesUntilDep = (int)Math.Floor(timeUntilDeparture.TotalMinutes);
            var secondsUntilDep = (int)Math.Floor(timeUntilDeparture.TotalSeconds);

            var totalTripSeconds = (RealtimeArrival - RealtimeDeparture).TotalSeconds;
            if (totalTripSeconds <= 0) totalTripSeconds = 600; // Fallback 10 min

            if (now < RealtimeDeparture)
            {
                // Bus has not left the origin yet
                if (minutesUntilDep <= 1 && secondsUntilDep > 0)
                {
                    TransitState = BusTransitState.BoardingSoon;
                    StatusBadge = "BOARDING NOW";
                    StatusColor = "#E81123"; // Accent Red
                    CountdownText = $"Departs in {secondsUntilDep}s";

                    // Countdown progress: approaching 100% departure readiness
                    ProgressPercent = Math.Min(100.0, Math.Max(0.0, (1.0 - (secondsUntilDep / 60.0)) * 100.0));
                    ProgressLabel = "Bus is at stop • Boarding";
                }
                else if (minutesUntilDep <= 0 && secondsUntilDep <= 0)
                {
                    TransitState = BusTransitState.BoardingSoon;
                    StatusBadge = "DEPARTING NOW";
                    StatusColor = "#E81123";
                    CountdownText = "Departing now";
                    ProgressPercent = 100.0;
                    ProgressLabel = "Departing stop";
                }
                else
                {
                    TransitState = BusTransitState.Upcoming;
                    StatusBadge = "UPCOMING";
                    StatusColor = "#0078D7"; // Windows Accent Blue

                    if (minutesUntilDep >= 60)
                    {
                        int hours = minutesUntilDep / 60;
                        int mins = minutesUntilDep % 60;
                        CountdownText = $"Departs in {hours}h {mins}m";
                    }
                    else
                    {
                        CountdownText = $"Departs in {minutesUntilDep} min";
                    }

                    // Progress bar indicates countdown window (within 30 mins)
                    double windowMinutes = 30.0;
                    double elapsedInWindow = Math.Max(0.0, windowMinutes - minutesUntilDep);
                    ProgressPercent = Math.Min(100.0, Math.Max(0.0, (elapsedInWindow / windowMinutes) * 100.0));
                    ProgressLabel = $"Origin: {FromStopName}";
                }
            }
            else if (now >= RealtimeDeparture && now < RealtimeArrival)
            {
                // Bus is currently in transit between origin and destination!
                TransitState = BusTransitState.InTransit;
                StatusBadge = "EN ROUTE";
                StatusColor = "#107C41"; // Active Green

                var elapsedTripSeconds = (now - RealtimeDeparture).TotalSeconds;
                var percent = Math.Min(100.0, Math.Max(0.0, (elapsedTripSeconds / totalTripSeconds) * 100.0));
                ProgressPercent = percent;

                var remainingMinutes = Math.Max(1, (int)Math.Ceiling((RealtimeArrival - now).TotalMinutes));
                CountdownText = $"En route • {remainingMinutes} min left";
                ProgressLabel = $"{Math.Round(percent)}% completed • Next: {ToStopName}";
            }
            else
            {
                // Bus has reached destination
                TransitState = BusTransitState.Completed;
                StatusBadge = "ARRIVED";
                StatusColor = "#666666"; // Neutral gray
                CountdownText = "Arrived at destination";
                ProgressPercent = 100.0;
                ProgressLabel = $"Arrived at {ToStopName}";
            }
        }

        private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(storage, value))
            {
                storage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
