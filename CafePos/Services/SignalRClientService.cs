using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public class SignalRClientService
    {
        private readonly HubConnection _connection;
        private readonly string _groupName;

        // Ekranda bir olay olduğunda UI tarafına haber vermek için Event (Olay) tanımları
        public event Action<string> OnOrderNotificationReceived;
        public event Action<int, string> OnOrderReadyStatusReceived;
        public event Action<int> OnTableClearedReceived;

        // Hangi ekran için (Kasa, Mutfak, Bar) başlatılacağını constructor'da alıyoruz
        public SignalRClientService(string groupName)
        {
            _groupName = groupName;

            // Kestrel sunucumuzun çalıştığı adresi veriyoruz (Kendine bağlanıyor)
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/posHub")
                .WithAutomaticReconnect() // İnternet koparsa otomatik tekrar bağlanmayı dener
                .Build();

            // Hub'dan (Sunucudan) gelen mesajları dinleyecek metodları bağlıyoruz
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            // PosHub'daki "ReceiveOrderNotification" tetiklendiğinde burası çalışır
            _connection.On<string>("ReceiveOrderNotification", (message) =>
            {
                // UI (Ekran) tarafına mesajı ilet
                OnOrderNotificationReceived?.Invoke(message);
            });

            _connection.On<int, string>("ReceiveOrderReadyStatus", (tableId, statusMessage) =>
            {
                OnOrderReadyStatusReceived?.Invoke(tableId, statusMessage);
            });

            _connection.On<int>("ReceiveTableCleared", (tableId) =>
            {
                OnTableClearedReceived?.Invoke(tableId);
            });
        }

        public async Task StartConnectionAsync()
        {
            try
            {
                await _connection.StartAsync();

                // Bağlantı başarılıysa, kendini ait olduğu gruba (Örn: Mutfak) kaydet
                await _connection.InvokeAsync("JoinGroup", _groupName);
            }
            catch (Exception ex)
            {
                // Burada bir Log mekanizması veya ekranda "Bağlantı Hatası" uyarısı çıkarılabilir
                Console.WriteLine($"SignalR Bağlantı Hatası: {ex.Message}");
            }
        }

        public async Task StopConnectionAsync()
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("LeaveGroup", _groupName);
                await _connection.StopAsync();
            }
        }

        // Bu ekran üzerinden sunucuya (diğer ekranlara) mesaj göndermek için yardımcı bir metod
        public async Task SendOrderReady(int tableId, string statusMessage)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("SendOrderReadyNotification", tableId, statusMessage);
            }
        }
    }
}