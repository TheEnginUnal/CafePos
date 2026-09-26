using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using CafePos.Models.Enums;

namespace CafePos.Models.Entities
{
    public class Table
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Empty;

        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
