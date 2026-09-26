// Models/Entities/TicketActionLog.cs
using System;
using CafePos.Models.Enums;
using CafePos.Models.Enums.CafePos.Models.Enums;

namespace CafePos.Models.Entities
{
    public class TicketActionLog
    {
        // Temel Bilgiler
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? TicketItemId { get; set; } // Yuvarlama (Rounding) tüm adisyona uygulandığı için bu alan null bırakılabilmelidir.
        public int AppUserId { get; set; }     // İşlemi yapan personelin ID'si.

        // İşlem Detayları
        public ActionType ActionType { get; set; } // 1: İkram, 2: İade, 3: Yuvarlama.
        public string ReasonCode { get; set; }     // Seçilen sabit sebep kodu (Örn: "Müşteri Memnuniyeti")[cite: 2].

        // Finansal Değerler
        public decimal OriginalValue { get; set; }   // İşlemden önceki fiyat/değer[cite: 2].
        public decimal AdjustmentValue { get; set; } // Düşülen veya eklenen tutar[cite: 2].
        public DateTime Timestamp { get; set; } = DateTime.Now; // İşlemin yapıldığı tam zaman[cite: 2].

        // Navigasyon Özellikleri
        public virtual Ticket Ticket { get; set; }
        public virtual TicketItem TicketItem { get; set; }
        public virtual AppUser AppUser { get; set; }
    }
}