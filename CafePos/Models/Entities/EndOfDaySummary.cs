using System;
using System.Collections.Generic;
using System.Text;

namespace CafePos.Models.Entities
{
    public class EndOfDaySummary
    {
        public int Id { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalCreditCard { get; set; }
        public decimal TotalTreats { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalRefunds { get; set; }
    }
}
