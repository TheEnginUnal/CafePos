using CafePos.Models.Entities;

namespace CafePos.Core
{
    public static class AppSession
    {
        // Şu an sistemi kullanan aktif personel
        public static AppUser CurrentUser { get; set; }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}