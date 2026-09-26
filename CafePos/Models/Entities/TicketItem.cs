// Models/Entities/TicketItem.cs
using System.Collections.Generic;
using CafePos.Models.Enums;

namespace CafePos.Models.Entities
{
    public class TicketItem
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public bool IsProcessed { get; set; } // İkram veya İade işlemi gördü mü?[cite: 2].
        public decimal FinalPrice { get; set; } // İşlem sonrası geçerli fiyat[cite: 2].

        public TicketItemStatus Status { get; set; } = TicketItemStatus.Preparing;
        public string? Note { get; set; }
        public bool IsTreat { get; set; } = false;
        public bool IsRefunded { get; set; } = false;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public virtual Ticket Ticket { get; set; }
        public virtual Product Product { get; set; }
        public DateTime? PreparationStartTime { get; set; } // Fırına atılma zamanı
        public virtual ICollection<TicketItemTag> Tags { get; set; } = new List<TicketItemTag>();

        // Bu ürüne ait loglar
        public virtual ICollection<TicketActionLog> ActionLogs { get; set; } = new List<TicketActionLog>();
    }
}