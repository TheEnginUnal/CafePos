using System;
using System.Collections.Generic;
using System.Text;

namespace CafePos.Models.Entities
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public bool IsGeneral { get; set; } = false;
        public int? ProductTypeId { get; set; }
        public int? ProductId { get; set; }
        public decimal AdditionalPrice { get; set; } = 0;

        public virtual ProductType ProductType { get; set; }
        public virtual Product Product { get; set; }
    }
}
