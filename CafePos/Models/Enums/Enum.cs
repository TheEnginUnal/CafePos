using System;
using System.Collections.Generic;
using System.Text;

namespace CafePos.Models.Enums
{
    public enum TargetScreen
    {
        None = 0,
        Kitchen = 1,
        Bar = 2
    }

    public enum TableStatus
    {
        Empty = 0,        // Gri
        Waiting = 1,      // Turuncu
        KitchenReady = 2, // Kırmızı
        BarReady = 3,     // Siyah
        Delivered = 4     // Yeşil
    }

    public enum TicketStatus
    {
        Open = 0,
        Paid = 1,
        Canceled = 2,
        Closed = 3
    }

    public enum TicketItemStatus
    {
        Preparing = 0,
        Ready = 1,
        Delivered = 2
    }

    public enum PaymentType
    {
        Cash = 0,
        CreditCard = 1
    }

    public enum TargetScreenType
    {
        None = 0,     // Kasadan direkt verilenler (Örn: Su)
        Kitchen = 1,  // Mutfağa gidecekler (Örn: Hamburger)
        Barista = 2   // Baristaya gidecekler (Örn: Latte)
    }

    public enum UserRole
    {
        Admin = 1,
        Cashier =2,
        Waiter = 3
    }
    namespace CafePos.Models.Enums
    {
        public enum ActionType
        {
            Treat = 1,      // İkram
            Refund = 2,     // İade
            Rounding = 3    // Yuvarlama
        }
    }
}
