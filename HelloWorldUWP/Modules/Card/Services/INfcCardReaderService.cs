using System;
using System.Threading.Tasks;
using HelloWorldUWP.Modules.Card.Models;

namespace HelloWorldUWP.Modules.Card.Services
{
    public interface INfcCardReaderService
    {
        bool IsNfcSupported { get; }
        bool IsListening { get; }
        event EventHandler<MensaCardData> CardDetected;
        event EventHandler<string> StatusChanged;
        event EventHandler<string> ErrorOccurred;
        event EventHandler<string> LogAppended;

        string CurrentLog { get; }
        void ClearLog();

        Task StartListeningAsync();
        void StopListening();
        MensaCardData GetLastCachedCard();
        MensaCardData SimulateCardReading(int? sampleBalanceCents = null);
    }
}
