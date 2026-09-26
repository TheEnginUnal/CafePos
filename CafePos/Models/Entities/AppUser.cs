// Models/Entities/AppUser.cs
using System.Collections.Generic;
using CafePos.Models.Enums;

namespace CafePos.Models.Entities
{
    public class AppUser
    {
        public int Id { get; set; }
        public string FullName { get; set; }    // Örn: Ahmet Yılmaz
        public string UserName { get; set; }    // Örn: ahmet (Giriş için kullanılabilir)
        public string PinCode { get; set; }     // Örn: "1453" (Hızlı giriş şifresi)

        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        // Bu personelin açtığı/ilgilendiği adisyonlar
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}