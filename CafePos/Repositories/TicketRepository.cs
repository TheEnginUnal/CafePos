using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;

namespace CafePos.Repositories
{
    // GenericRepository'nin tüm özelliklerini (Ekle, Sil vb.) miras alıyoruz
    public class TicketRepository : GenericRepository<Ticket>
    {
        // Masanın aktif (açık) olan adisyonunu, içindeki ürünler ve ürün etiketleriyle beraber getirir
        public async Task<Ticket> GetActiveTicketByTableIdAsync(int tableId)
        {
            using (var context = new AppDbContext())
            {
                return await context.Tickets
                    .Include(t => t.TicketItems)
                        .ThenInclude(ti => ti.Product) // Siparişin içindeki ürün bilgisini de getir
                    .Include(t => t.TicketItems)
                        .ThenInclude(ti => ti.Tags) // Siparişe eklenmiş (Antepli vb.) etiketleri de getir
                    .Where(t => t.TableId == tableId && t.Status == TicketStatus.Open)
                    .FirstOrDefaultAsync();
            }
        }

        // Ödeme alındığında fişi kapatan ve masayı boşaltan özel metod
        public async Task CloseTicketAsync(int ticketId, decimal roundingAmount)
        {
            using (var context = new AppDbContext())
            {
                var ticket = await context.Tickets
                                          .Include(t => t.Table)
                                          .FirstOrDefaultAsync(t => t.Id == ticketId);
                if (ticket != null)
                {
                    ticket.Status = TicketStatus.Paid;
                    ticket.ClosedAt = System.DateTime.Now;
                    ticket.RoundingAmount = roundingAmount;

                    // Masayı boşa (Gri) çekiyoruz
                    ticket.Table.Status = TableStatus.Empty;

                    context.Tickets.Update(ticket);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}