// Models/Entities/Ticket.cs
using System;
using System.Collections.Generic;
using CafePos.Models.Enums;

namespace CafePos.Models.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ClosedAt { get; set; }
        public string? Note { get; set; }

        public decimal RoundingAmount { get; set; } = 0; // İndirim ile ödenen tutar arasındaki yuvarlama farkı[cite: 2].
        public int? RoundingUserId { get; set; }         // Yuvarlamayı uygulayan personelin ID'si[cite: 2].

        public decimal DiscountAmount { get; set; } = 0;
        public decimal DiscountPercentage { get; set; } = 0;

        // Adisyonu Açan / İlgilenen Personel
        public int? AppUserId { get; set; }

        public virtual AppUser AppUser { get; set; }
        public virtual AppUser RoundingUser { get; set; }
        public virtual Table Table { get; set; }
        public virtual ICollection<TicketItem> TicketItems { get; set; } = new List<TicketItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Bu adisyona ait loglar
        public virtual ICollection<TicketActionLog> ActionLogs { get; set; } = new List<TicketActionLog>();
    }
}