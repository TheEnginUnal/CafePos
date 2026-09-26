using System;
using System.Collections.Generic;
using System.Text;
using CafePos.Models.Enums;

namespace CafePos.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        // Yabancı Anahtarlar (Foreign Keys)
        public int CategoryId { get; set; }
        public int ProductTypeId { get; set; }

        // POS İşlem Özellikleri
        public TargetScreenType TargetScreen { get; set; } // Hata vermemesi için tipi TargetScreenType yaptık

        // Stok Özellikleri
        public bool IsStockTracked { get; set; } // Stok takibi yapılsın mı? (Evet/Hayır)
        public int? StockCount { get; set; }     // Eğer yapılıyorsa mevcut stok adedi

        // Durum
        public bool IsActive { get; set; } = true;

        // Navigasyon Özellikleri (İlişkiler)
        public virtual Category Category { get; set; }
        public virtual ProductType ProductType { get; set; }
    }
}