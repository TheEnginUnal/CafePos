using System;
using System.Collections.Generic;
using System.Text;

namespace CafePos.Models.Entities
{
    public class TicketItemTag
    {
        public int Id { get; set; }
        public int TicketItemId { get; set; }

        public string TagName { get; set; }
        public decimal Price { get; set; } = 0;

        public virtual TicketItem TicketItem { get; set; }
    }
}
