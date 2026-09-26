using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CafePos.Hubs
{
    // Hub sınıfı SignalR üzerinden tüm iletişimi yönetir
    public class PosHub : Hub
    {
        // 1. İstemci (Mutfak, Kasa vb.) bağlandığında gruba ekleme
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // 2. İstemci ayrıldığında gruptan çıkarma
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // 3. Yeni sipariş geldiğinde ilgili gruba mesaj fırlatma
        public async Task SendNewOrderNotification(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveOrderNotification", message);
        }

        // 4. Sipariş hazır olduğunda Kasaya durumu bildirme
        public async Task SendOrderReadyNotification(int tableId, string statusMessage)
        {
            await Clients.Group("Kasa").SendAsync("ReceiveOrderReadyStatus", tableId, statusMessage);
        }

        // 5. Masanın tamamen boşaldığını (ödeme alındığını) herkese bildirme
        public async Task SendTableClearedNotification(int tableId)
        {
            await Clients.All.SendAsync("ReceiveTableCleared", tableId);
        }
    }
}