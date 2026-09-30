using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SmartCards;
using Windows.Networking.Proximity;
using Windows.Storage;
using Windows.Storage.Streams;
using HelloWorldUWP.Modules.Card.Models;

namespace HelloWorldUWP.Modules.Card.Services
{
    public class NfcCardReaderService : INfcCardReaderService
    {
        private const string CacheKeyBalance = "CachedCard_Balance";
        private const string CacheKeyTime = "CachedCard_Time";
        private const string CacheKeyType = "CachedCard_Type";

        private class FastAidTarget
        {
            public byte[] Aid { get; }
            public string Label { get; }
            public FastAidTarget(byte[] aid, string label)
            {
                Aid = aid;
                Label = label;
            }
        }

        private SmartCardReader _smartCardReader;
        private ProximityDevice _proximityDevice;
        private bool _isNfcSupported;
        private bool _isListening;
        private readonly StringBuilder _logBuilder = new StringBuilder();

        public bool IsNfcSupported => _isNfcSupported;
        public bool IsListening => _isListening;
        public string CurrentLog
        {
            get
            {
                lock (_logBuilder)
                {
                    return _logBuilder.ToString();
                }
            }
        }

        public event EventHandler<MensaCardData> CardDetected;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> LogAppended;

        public NfcCardReaderService()
        {
            CheckHardwareSupport();
        }

        public void ClearLog()
        {
            lock (_logBuilder)
            {
                _logBuilder.Clear();
            }
            LogAppended?.Invoke(this, "");
        }

        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string line = $"[{timestamp}] {message}";

            // Immediate output to Visual Studio 2017 debugger Output window
            System.Diagnostics.Debug.WriteLine($"[SaarlandUni NFC] {line}");

            lock (_logBuilder)
            {
                _logBuilder.AppendLine(line);
                if (_logBuilder.Length > 80000)
                {
                    _logBuilder.Remove(0, 30000);
                }
            }
            LogAppended?.Invoke(this, line);
        }

        private void CheckHardwareSupport()
        {
            try
            {
                _proximityDevice = ProximityDevice.GetDefault();
                _isNfcSupported = _proximityDevice != null;
            }
            catch
            {
                _isNfcSupported = false;
            }
        }

        public async Task StartListeningAsync()
        {
            if (_isListening) return;

            try
            {
                Log("Starting NFC smart card service...");
                StatusChanged?.Invoke(this, "Searching for NFC smart card reader...");

                // Find NFC SmartCard Reader
                string selector = SmartCardReader.GetDeviceSelector(SmartCardReaderKind.Nfc);
                var devices = await DeviceInformation.FindAllAsync(selector);

                if (devices != null && devices.Count > 0)
                {
                    _isNfcSupported = true;
                    var device = devices[0];
                    Log($"Found NFC Reader Device: '{device.Name}' ({device.Id})");
                    _smartCardReader = await SmartCardReader.FromIdAsync(device.Id);
                    if (_smartCardReader != null)
                    {
                        _smartCardReader.CardAdded += SmartCardReader_CardAdded;
                        _isListening = true;
                        StatusChanged?.Invoke(this, "NFC Reader active • Hold your UdS-Card to phone");
                        Log("Reader attached. Listening for physical smart card taps...");
                        return;
                    }
                }

                // If dedicated SmartCardReader selector returned empty, try proximity device
                if (_proximityDevice != null)
                {
                    _isListening = true;
                    StatusChanged?.Invoke(this, "NFC Ready • Tap your UdS-Card against the back of your phone");
                    Log("Proximity device ready for tap events.");
                    return;
                }

                // Fallback state if phone has no NFC hardware
                _isListening = false;
                StatusChanged?.Invoke(this, "NFC hardware is not available on this device (You can use 'Simulate Tap' to test)");
                Log("No NFC hardware found on this device.");
            }
            catch (Exception ex)
            {
                _isListening = false;
                StatusChanged?.Invoke(this, "NFC initialization error");
                ErrorOccurred?.Invoke(this, $"NFC initialization error: {ex.Message}");
                Log($"Initialization error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void StopListening()
        {
            if (!_isListening) return;

            try
            {
                if (_smartCardReader != null)
                {
                    _smartCardReader.CardAdded -= SmartCardReader_CardAdded;
                    _smartCardReader = null;
                }
            }
            catch { }

            _isListening = false;
            StatusChanged?.Invoke(this, "Reader paused");
            Log("NFC reader listener stopped.");
        }

        private async void SmartCardReader_CardAdded(SmartCardReader sender, CardAddedEventArgs args)
        {
            try
            {
                StatusChanged?.Invoke(this, "Card detected! Reading balance...");
                Log("==================================================");
                Log(">>> SMART CARD DETECTED (CardAdded Event) <<<");

                var card = args.SmartCard;
                if (card == null)
                {
                    Log("args.SmartCard returned null!");
                    return;
                }

                Log($"Reader DeviceId: {sender?.DeviceId ?? "(unknown)"}");

                SmartCardConnection connection = null;
                try
                {
                    Log("Attempting card.ConnectAsync()...");
                    connection = await card.ConnectAsync();
                    Log("card.ConnectAsync() connected successfully!");
                }
                catch (Exception ex)
                {
                    Log($"card.ConnectAsync() FAILED: {ex.GetType().Name}: {ex.Message} (0x{ex.HResult:X8})");
                    System.Diagnostics.Debug.WriteLine($"[SaarlandUni NFC EXCEPTION] ConnectAsync failed: {ex}");
                    StatusChanged?.Invoke(this, "Failed to connect to card");
                    ErrorOccurred?.Invoke(this, $"Connect error: {ex.Message}");
                    return;
                }

                using (connection)
                {
                    var result = await ReadCardWithDiagnosticsAsync(card, connection);

                    if (result != null)
                    {
                        SaveToCache(result);
                        CardDetected?.Invoke(this, result);
                        StatusChanged?.Invoke(this, $"Card read successfully ({result.FormattedBalance})");
                    }
                    else
                    {
                        StatusChanged?.Invoke(this, "Card read finished (no balance decoded)");
                        ErrorOccurred?.Invoke(this, "Could not decode balance. Please check the Diagnostic APDU Log or VS Debug Output.");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, "Card read failed");
                ErrorOccurred?.Invoke(this, $"Card read exception: {ex.Message}");
                Log($"CardAdded Exception: {ex.GetType().FullName}: {ex.Message} (0x{ex.HResult:X8})\n{ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[SaarlandUni NFC EXCEPTION] CardAdded: {ex}");
            }
            finally
            {
                Log(">>> CARD SESSION CLOSED <<<");
                Log("==================================================");
            }
        }

        private string FormatHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "(none)";
            return BitConverter.ToString(bytes).Replace('-', ' ');
        }

        private string ExplainStatus(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 2) return "";
            byte sw1 = bytes[bytes.Length - 2];
            byte sw2 = bytes[bytes.Length - 1];

            if (sw1 == 0x91 && sw2 == 0x00) return " [DESFire: OK / SUCCESS]";
            if (sw1 == 0x91 && sw2 == 0xAF) return " [DESFire: MORE FRAMES AVAILABLE]";
            if (sw1 == 0x91 && sw2 == 0xA0) return " [DESFire: APPLICATION NOT FOUND]";
            if (sw1 == 0x91 && sw2 == 0xF0) return " [DESFire: FILE NOT FOUND]";
            if (sw1 == 0x91 && sw2 == 0x9D) return " [DESFire: PERMISSION DENIED / LOCKED]";
            if (sw1 == 0x91 && sw2 == 0x7E) return " [DESFire: LENGTH ERROR]";
            if (sw1 == 0x91 && sw2 == 0x1C) return " [DESFire: ILLEGAL COMMAND CODE]";
            if (sw1 == 0x91 && sw2 == 0x9E) return " [DESFire: PARAMETER ERROR]";
            if (sw1 == 0x91 && sw2 == 0xAE) return " [DESFire: AUTHENTICATION ERROR]";
            if (sw1 == 0x90 && sw2 == 0x00) return " [ISO 7816: SUCCESS]";
            if (sw1 == 0x6E && sw2 == 0x00) return " [ISO 7816: CLA NOT SUPPORTED]";
            if (sw1 == 0x6D && sw2 == 0x00) return " [ISO 7816: INS NOT SUPPORTED]";
            if (sw1 == 0x67 && sw2 == 0x00) return " [ISO 7816: WRONG LENGTH (Le/Lc)]";
            if (sw1 == 0x6A && sw2 == 0x82) return " [ISO 7816: FILE/APP NOT FOUND]";
            if (sw1 == 0x6A && sw2 == 0x86) return " [ISO 7816: INCORRECT P1-P2]";
            if (sw1 == 0x69 && sw2 == 0x82) return " [ISO 7816: SECURITY STATUS NOT SATISFIED]";

            return $" [SW: {sw1:X2}{sw2:X2}]";
        }

        private async Task<byte[]> TransmitApduAsync(SmartCardConnection connection, string label, byte[] apdu)
        {
            string sendHex = FormatHex(apdu);
            Log($"-> [{label}] TX ({apdu.Length}B): {sendHex}");
            try
            {
                var respBuffer = await connection.TransmitAsync(apdu.AsBuffer());
                byte[] respBytes = respBuffer.ToArray();
                string respHex = FormatHex(respBytes);
                string annotation = ExplainStatus(respBytes);
                Log($"<- [{label}] RX ({respBytes.Length}B): {respHex}{annotation}");
                return respBytes;
            }
            catch (Exception ex)
            {
                string err = $"<! [{label}] TRANSMIT ERROR: {ex.GetType().Name}: {ex.Message} (0x{ex.HResult:X8})";
                Log(err);
                System.Diagnostics.Debug.WriteLine($"[SaarlandUni NFC TRANSMIT ERROR] {err}\n{ex.StackTrace}");
                return null;
            }
        }

        private bool IsDesfireSuccess(byte[] resp)
        {
            if (resp == null || resp.Length < 2) return false;
            byte sw1 = resp[resp.Length - 2];
            byte sw2 = resp[resp.Length - 1];
            return (sw1 == 0x91 && sw2 == 0x00) || (sw1 == 0x90 && sw2 == 0x00);
        }

        private async Task<MensaCardData> ReadCardWithDiagnosticsAsync(SmartCard card, SmartCardConnection connection)
        {
            // FAST PATH: Select InterCard Purse directly (Takes < 40ms)
            // This prevents RF field drop / timeouts before balance is read
            Log(">>> Executing FAST-PATH: InterCard Purse Selection <<<");

            var fastAids = new[]
            {
                new FastAidTarget(new byte[] { 0x5F, 0x84, 0x15 }, "InterCard Purse (5F 84 15)"),
                new FastAidTarget(new byte[] { 0x15, 0x84, 0x5F }, "InterCard Purse (15 84 5F)"),
                new FastAidTarget(new byte[] { 0x5F, 0x84, 0x25 }, "InterCard Purse 2 (5F 84 25)"),
                new FastAidTarget(new byte[] { 0x25, 0x84, 0x5F }, "InterCard Purse 2 (25 84 5F)")
            };

            foreach (var fast in fastAids)
            {
                byte[] selCmd = new byte[] { 0x90, 0x5A, 0x00, 0x00, 0x03, fast.Aid[0], fast.Aid[1], fast.Aid[2], 0x00 };
                byte[] selResp = await TransmitApduAsync(connection, $"FAST SELECT {fast.Label}", selCmd);

                if (IsDesfireSuccess(selResp))
                {
                    Log($"*** [FAST SUCCESS] {fast.Label} SELECTED! ***");

                    // Read File IDs in this application
                    byte[] filesResp = await TransmitApduAsync(connection, "FAST Get File IDs", new byte[] { 0x90, 0x6F, 0x00, 0x00, 0x00 });
                    var fileIds = new List<byte>();
                    if (filesResp != null && filesResp.Length >= 2 && IsDesfireSuccess(filesResp))
                    {
                        for (int f = 0; f < filesResp.Length - 2; f++)
                        {
                            fileIds.Add(filesResp[f]);
                            Log($"  File #{f + 1}: 0x{filesResp[f]:X2}");
                        }
                    }

                    if (!fileIds.Contains(0x01)) fileIds.Add(0x01);
                    if (!fileIds.Contains(0x02)) fileIds.Add(0x02);

                    foreach (var fileId in fileIds)
                    {
                        // Try ReadValue (0x6C)
                        byte[] valResp = await TransmitApduAsync(connection, $"FAST ReadValue File {fileId}", new byte[] { 0x90, 0x6C, 0x00, 0x00, 0x01, fileId, 0x00 });
                        if (valResp != null && valResp.Length >= 6 && IsDesfireSuccess(valResp))
                        {
                            int rawUnits = valResp[0] | (valResp[1] << 8) | (valResp[2] << 16) | (valResp[3] << 24);
                            // InterCard stores balance in tenths of a cent (0.1 cent = 1/1000 EUR)
                            // Example: raw 9410 units / 10 = 941 cents = 9,41 €
                            int cents = rawUnits / 10;
                            Log($"****************************************************************");
                            Log($"*** [BALANCE FOUND!] raw={rawUnits} -> {cents} cents ({cents / 100.0:F2} €) from File {fileId}! ***");
                            Log($"****************************************************************");

                            return new MensaCardData
                            {
                                BalanceInCents = cents,
                                ReadTimestamp = DateTime.Now,
                                CardType = "UdS-Card (MIFARE DESFire EV1)",
                                SystemVendor = "InterCard / Secanda",
                                IsSimulated = false
                            };
                        }

                        // Try ReadData (0xBD) 32 bytes
                        byte[] dataResp = await TransmitApduAsync(connection, $"FAST ReadData File {fileId}", new byte[] { 0x90, 0xBD, 0x00, 0x00, 0x07, fileId, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00 });
                        if (dataResp != null && dataResp.Length >= 6 && IsDesfireSuccess(dataResp))
                        {
                            Log($"File {fileId} Data: {FormatHex(dataResp)}");
                            if (dataResp.Length >= 10)
                            {
                                int low = dataResp[7] & 0xFF;
                                int hi = dataResp[6] & 0xFF;
                                int mcCents = (hi << 8 | low) * 10;
                                if (mcCents > 0 && mcCents < 50000)
                                {
                                    Log($"*** [BALANCE FOUND FROM DATA!] {mcCents} cents ({mcCents / 100.0:F2} €) ***");
                                    return new MensaCardData
                                    {
                                        BalanceInCents = mcCents,
                                        ReadTimestamp = DateTime.Now,
                                        CardType = "UdS-Card (InterCard Data)",
                                        SystemVendor = "InterCard / Secanda",
                                        IsSimulated = false
                                    };
                                }
                            }
                        }
                    }
                }
            }

            Log(">>> Fast-Path did not find balance, running exploratory diagnostics <<<");

            // 1. Query Card Status
            try
            {
                var status = await card.GetStatusAsync();
                Log($"Card Status: {status}");
            }
            catch (Exception ex)
            {
                Log($"GetStatus exception: {ex.Message}");
            }

            // 2. Query Card ATR
            try
            {
                var atr = await card.GetAnswerToResetAsync();
                if (atr != null)
                {
                    byte[] atrBytes = atr.ToArray();
                    Log($"Card ATR ({atrBytes.Length}B): {FormatHex(atrBytes)}");
                }
            }
            catch (Exception ex)
            {
                Log($"GetAnswerToReset exception: {ex.Message}");
            }

            // 3. DESFire GetVersion (INS=0x60)
            byte[] verResp = await TransmitApduAsync(connection, "DESFire GetVersion (Frame 1)", new byte[] { 0x90, 0x60, 0x00, 0x00, 0x00 });
            if (verResp != null && verResp.Length >= 2 && verResp[verResp.Length - 2] == 0x91 && verResp[verResp.Length - 1] == 0xAF)
            {
                await TransmitApduAsync(connection, "DESFire GetVersion (Frame 2)", new byte[] { 0x90, 0xAF, 0x00, 0x00, 0x00 });
                await TransmitApduAsync(connection, "DESFire GetVersion (Frame 3)", new byte[] { 0x90, 0xAF, 0x00, 0x00, 0x00 });
            }

            // 4. DESFire GetApplicationIDs (INS=0x6A)
            var discoveredAids = new List<byte[]>();
            byte[] aidsResp = await TransmitApduAsync(connection, "DESFire List AIDs", new byte[] { 0x90, 0x6A, 0x00, 0x00, 0x00 });

            while (aidsResp != null && aidsResp.Length >= 2 && aidsResp[aidsResp.Length - 2] == 0x91 && (aidsResp[aidsResp.Length - 1] == 0x00 || aidsResp[aidsResp.Length - 1] == 0xAF))
            {
                int payloadLen = aidsResp.Length - 2;
                int count = payloadLen / 3;
                for (int i = 0; i < count; i++)
                {
                    byte[] aid = new byte[] { aidsResp[i * 3], aidsResp[i * 3 + 1], aidsResp[i * 3 + 2] };
                    discoveredAids.Add(aid);
                    int aidVal = aid[0] | (aid[1] << 8) | (aid[2] << 16);
                    Log($"Discovered Card Application AID: {FormatHex(aid)} (0x{aidVal:X6})");
                }

                if (aidsResp[aidsResp.Length - 1] == 0xAF)
                {
                    aidsResp = await TransmitApduAsync(connection, "DESFire List AIDs (Next Frame)", new byte[] { 0x90, 0xAF, 0x00, 0x00, 0x00 });
                }
                else
                {
                    break;
                }
            }

            // Helper to add candidate AIDs if not already in list
            void AddAid(byte[] candidate)
            {
                if (!discoveredAids.Any(a => a[0] == candidate[0] && a[1] == candidate[1] && a[2] == candidate[2]))
                {
                    discoveredAids.Add(candidate);
                }
            }

            // Add standard university & InterCard candidate AIDs
            AddAid(new byte[] { 0x15, 0x84, 0x5F }); // InterCard 0x5F8415 (Little-Endian)
            AddAid(new byte[] { 0x5F, 0x84, 0x15 }); // InterCard 0x5F8415 (Big-Endian)
            AddAid(new byte[] { 0x01, 0x14, 0x01 }); // InterCard System 0x011401
            AddAid(new byte[] { 0xF3, 0x80, 0xF0 }); // MagnaCarta 0xF080F3 (Little-Endian)
            AddAid(new byte[] { 0xF0, 0x80, 0xF3 }); // MagnaCarta 0xF080F3 (Big-Endian)
            AddAid(new byte[] { 0x00, 0x00, 0x00 }); // Master PICC Application

            // Sort discovered AIDs so InterCard purse (5F 84 15) and secondary (5F 84 25) come FIRST
            var orderedAids = discoveredAids
                .OrderBy(a => (a[0] == 0x5F && a[1] == 0x84 && a[2] == 0x15) ? 0 :
                              (a[0] == 0x5F && a[1] == 0x84 && a[2] == 0x25) ? 1 :
                              (a[0] == 0x5F && a[1] == 0x84 && a[2] == 0x05) ? 2 : 10)
                .ToList();

            MensaCardData detectedCard = null;

            // 6. Test each candidate AID
            foreach (var aid in orderedAids)
            {
                int aidVal = aid[0] | (aid[1] << 8) | (aid[2] << 16);
                string aidStr = $"0x{aidVal:X6} [{FormatHex(aid)}]";

                // Standard ISO 7816-4 Wrapped DESFire SelectApplication (9 bytes: CLA=90, INS=5A, P1=00, P2=00, Lc=03, AID, Le=00)
                byte[] selectApdu = new byte[] { 0x90, 0x5A, 0x00, 0x00, 0x03, aid[0], aid[1], aid[2], 0x00 };
                byte[] selResp = await TransmitApduAsync(connection, $"SELECT AID {aidStr}", selectApdu);

                // If not success, try reversed byte order (Big-Endian vs Little-Endian)
                if (!IsDesfireSuccess(selResp))
                {
                    byte[] selectApduRev = new byte[] { 0x90, 0x5A, 0x00, 0x00, 0x03, aid[2], aid[1], aid[0], 0x00 };
                    selResp = await TransmitApduAsync(connection, $"SELECT AID {aidStr} (Reversed)", selectApduRev);
                }

                if (IsDesfireSuccess(selResp))
                {
                    Log($"*** SELECTED APPLICATION {aidStr} SUCCESSFULLY! ***");

                    // Query File IDs in this application
                    byte[] filesResp = await TransmitApduAsync(connection, $"Get File IDs in {aidStr}", new byte[] { 0x90, 0x6F, 0x00, 0x00, 0x00 });
                    var fileIds = new List<byte>();

                    if (filesResp != null && filesResp.Length >= 2 && IsDesfireSuccess(filesResp))
                    {
                        int fileCount = filesResp.Length - 2;
                        Log($"Found {fileCount} file(s) in application {aidStr}:");
                        for (int f = 0; f < fileCount; f++)
                        {
                            fileIds.Add(filesResp[f]);
                            Log($"  File #{f + 1}: 0x{filesResp[f]:X2} (File {filesResp[f]})");
                        }
                    }

                    // Ensure we check standard files 1, 2, 0 even if file enumeration is restricted
                    if (!fileIds.Contains(0x01)) fileIds.Add(0x01);
                    if (!fileIds.Contains(0x02)) fileIds.Add(0x02);
                    if (!fileIds.Contains(0x00)) fileIds.Add(0x00);

                    foreach (var fileId in fileIds)
                    {
                        // File Settings (0xF5)
                        await TransmitApduAsync(connection, $"Get Settings File {fileId}", new byte[] { 0x90, 0xF5, 0x00, 0x00, 0x01, fileId, 0x00 });

                        // Try ReadValue (0x6C) Case 4
                        byte[] valResp = await TransmitApduAsync(connection, $"ReadValue File {fileId} (Le=00)", new byte[] { 0x90, 0x6C, 0x00, 0x00, 0x01, fileId, 0x00 });
                        if (!IsDesfireSuccess(valResp))
                        {
                            valResp = await TransmitApduAsync(connection, $"ReadValue File {fileId} (Le=04)", new byte[] { 0x90, 0x6C, 0x00, 0x00, 0x01, fileId, 0x04 });
                        }

                        if (valResp != null && valResp.Length >= 6 && IsDesfireSuccess(valResp))
                        {
                            int rawUnits = valResp[0] | (valResp[1] << 8) | (valResp[2] << 16) | (valResp[3] << 24);
                            // InterCard stores balance in tenths of a cent (0.1 cent = 1/1000 EUR)
                            int cents = rawUnits / 10;
                            Log($"****************************************************************");
                            Log($"*** [BALANCE FOUND!] raw={rawUnits} -> {cents} cents ({cents / 100.0:F2} €) from AID {aidStr}, File {fileId}! ***");
                            Log($"****************************************************************");

                            if (detectedCard == null)
                            {
                                detectedCard = new MensaCardData
                                {
                                    BalanceInCents = cents,
                                    ReadTimestamp = DateTime.Now,
                                    CardType = "UdS-Card (MIFARE DESFire EV1)",
                                    SystemVendor = (aid[0] == 0x15 && aid[1] == 0x84 && aid[2] == 0x5F) ? "InterCard / Secanda" : $"AID {aidStr}",
                                    IsSimulated = false
                                };
                            }
                        }

                        // Try ReadData (0xBD) - read up to 32 bytes from offset 0
                        byte[] dataResp = await TransmitApduAsync(connection, $"ReadData File {fileId} (32B)", new byte[] { 0x90, 0xBD, 0x00, 0x00, 0x07, fileId, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00 });
                        if (dataResp != null && dataResp.Length >= 6 && IsDesfireSuccess(dataResp))
                        {
                            Log($"ReadData File {fileId} content: {FormatHex(dataResp)}");

                            // Check for MagnaCarta 16-bit encoding if not yet parsed
                            if (detectedCard == null && dataResp.Length >= 10)
                            {
                                int low = dataResp[7] & 0xFF;
                                int hi = dataResp[6] & 0xFF;
                                int mcCents = (hi << 8 | low) * 10;
                                if (mcCents > 0 && mcCents < 50000)
                                {
                                    Log($"*** [MAGNACARTA CANDIDATE] Balance: {mcCents} cents ({mcCents / 100.0:F2} €) ***");
                                    detectedCard = new MensaCardData
                                    {
                                        BalanceInCents = mcCents,
                                        ReadTimestamp = DateTime.Now,
                                        CardType = "Mensa Card (MagnaCarta)",
                                        SystemVendor = "MagnaCarta",
                                        IsSimulated = false
                                    };
                                }
                            }
                        }
                    }

                    if (detectedCard != null)
                    {
                        Log($"Balance detected ({detectedCard.FormattedBalance}). Successfully finished scan.");
                        break;
                    }
                }
            }

            // 7. Fallback: ISO 7816-4 direct selection (CLA=0x00)
            if (detectedCard == null)
            {
                Log("Testing ISO 7816-4 direct SELECT FILE (CLA=0x00)...");
                await TransmitApduAsync(connection, "ISO SELECT InterCard LE", new byte[] { 0x00, 0xA4, 0x04, 0x00, 0x03, 0x15, 0x84, 0x5F });
                await TransmitApduAsync(connection, "ISO SELECT InterCard BE", new byte[] { 0x00, 0xA4, 0x04, 0x00, 0x03, 0x5F, 0x84, 0x15 });
                await TransmitApduAsync(connection, "ISO SELECT NDEF Type 4", new byte[] { 0x00, 0xA4, 0x04, 0x00, 0x07, 0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01 });
            }

            return detectedCard;
        }

        public MensaCardData SimulateCardReading(int? sampleBalanceCents = null)
        {
            int balance = sampleBalanceCents ?? new Random().Next(350, 2450);

            Log("==================================================");
            Log(">>> SIMULATED CARD READING TRIGGERED <<<");
            Log("Card Status: Ready");
            Log("Card ATR (17B): 3B 81 80 01 80 4F 0C A0 00 00 03 06 03 00 03 00 00 00 [ISO 14443-4 DESFire EV1]");
            Log("-> [PC/SC GET UID] TX (5B): FF CA 00 00 00");
            Log("<- [PC/SC GET UID] RX (9B): 04 8A 3F 22 5A 6B 80 90 00 [ISO 7816: SUCCESS]");
            Log("Card UID (7B): 04 8A 3F 22 5A 6B 80");
            Log("-> [DESFire GetVersion (Frame 1)] TX (5B): 90 60 00 00 00");
            Log("<- [DESFire GetVersion (Frame 1)] RX (9B): 04 01 01 01 00 18 05 91 AF [DESFire: MORE FRAMES AVAILABLE]");
            Log("-> [DESFire List AIDs] TX (5B): 90 6A 00 00 00");
            Log("<- [DESFire List AIDs] RX (5B): 15 84 5F 91 00 [DESFire: OK / SUCCESS]");
            Log("Discovered Card Application AID: 15 84 5F (0x5F8415)");
            Log("-> [SELECT AID 0x5F8415 [15 84 5F] (Case 3)] TX (8B): 90 5A 00 00 03 15 84 5F");
            Log("<- [SELECT AID 0x5F8415 [15 84 5F] (Case 3)] RX (2B): 91 00 [DESFire: OK / SUCCESS]");
            Log("*** SELECTED APPLICATION 0x5F8415 [15 84 5F] SUCCESSFULLY! ***");
            Log("-> [Get File IDs in 0x5F8415] TX (5B): 90 6F 00 00 00");
            Log("<- [Get File IDs in 0x5F8415] RX (3B): 01 91 00 [DESFire: OK / SUCCESS]");
            Log("Found 1 file(s) in application: File #1: 0x01 (File 1)");

            byte b0 = (byte)(balance & 0xFF);
            byte b1 = (byte)((balance >> 8) & 0xFF);
            byte b2 = (byte)((balance >> 16) & 0xFF);
            byte b3 = (byte)((balance >> 24) & 0xFF);

            Log($"-> [ReadValue File 1 (Le=00)] TX (7B): 90 6C 00 00 01 01 00");
            Log($"<- [ReadValue File 1 (Le=00)] RX (6B): {b0:X2} {b1:X2} {b2:X2} {b3:X2} 91 00 [DESFire: OK / SUCCESS]");
            Log($"*** [BALANCE FOUND!] {balance} cents ({balance / 100.0:F2} €) from AID 0x5F8415, File 1! ***");
            Log(">>> SIMULATED SESSION COMPLETE <<<");
            Log("==================================================");

            var card = new MensaCardData
            {
                BalanceInCents = balance,
                ReadTimestamp = DateTime.Now,
                CardType = "UdS-Card (MIFARE DESFire EV1)",
                SystemVendor = "InterCard / Secanda",
                IsSimulated = true
            };

            SaveToCache(card);
            CardDetected?.Invoke(this, card);
            StatusChanged?.Invoke(this, $"Simulated card tapped ({card.FormattedBalance})");

            return card;
        }

        public MensaCardData GetLastCachedCard()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (settings.ContainsKey(CacheKeyBalance) && settings[CacheKeyBalance] is int balance)
                {
                    DateTime time = DateTime.Now;
                    if (settings.ContainsKey(CacheKeyTime) && settings[CacheKeyTime] is string timeStr)
                    {
                        DateTime.TryParse(timeStr, out time);
                    }

                    string type = settings.ContainsKey(CacheKeyType) ? settings[CacheKeyType] as string : "UdS-Card";

                    return new MensaCardData
                    {
                        BalanceInCents = balance,
                        ReadTimestamp = time,
                        CardType = type ?? "UdS-Card (MIFARE DESFire EV1)",
                        SystemVendor = "InterCard / Secanda",
                        IsSimulated = false
                    };
                }
            }
            catch { }

            return null;
        }

        private void SaveToCache(MensaCardData data)
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                settings[CacheKeyBalance] = data.BalanceInCents;
                settings[CacheKeyTime] = data.ReadTimestamp.ToString("o");
                settings[CacheKeyType] = data.CardType;
            }
            catch { }
        }
    }
}
