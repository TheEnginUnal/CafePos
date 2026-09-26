using System;
using System.Collections.Generic;
using System.Text;
using CafePos.Models.Enums;

namespace CafePos.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Ticket Ticket { get; set; }
    }
}
