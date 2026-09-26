using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;

namespace CafePos.Views
{
    public partial class BaristaPage : UserControl
    {
        public ObservableCollection<BaristaTicketModel> BaristaTickets { get; set; }
        private DispatcherTimer _uiTimer;

        public BaristaPage()
        {
            InitializeComponent();
            BaristaTickets = new ObservableCollection<BaristaTicketModel>();
            BaristaOrdersItemsControl.ItemsSource = BaristaTickets;

            // Timer her 10 saniyede bir UI'ı günceller
            _uiTimer = new DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromSeconds(10);
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            this.Loaded += BaristaPage_Loaded;
        }

        private async void BaristaPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadBaristaOrdersAsync();
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            // Tüm barista adisyonlarının ekrandaki sürelerini otomatik güncelle
            foreach (var ticket in BaristaTickets)
            {
                ticket.UpdateTimerText();
            }
        }

        private async Task LoadBaristaOrdersAsync()
        {
            using (var db = new AppDbContext())
            {
                var activeTickets = await db.Tickets
                    .Include(t => t.Table)
                    .Include(t => t.TicketItems)
                        .ThenInclude(ti => ti.Product)
                    .Where(t => t.Status == TicketStatus.Open &&
                                t.TicketItems.Any(ti => ti.Status == TicketItemStatus.Preparing && ti.Product.TargetScreen == TargetScreenType.Barista))
                    .ToListAsync();

                BaristaTickets.Clear();

                foreach (var ticket in activeTickets)
                {
                    var baristaItems = ticket.TicketItems
                        .Where(ti => ti.Status == TicketItemStatus.Preparing && ti.Product.TargetScreen == TargetScreenType.Barista)
                        .Select(ti => new BaristaItemModel
                        {
                            TicketItemId = ti.Id,
                            ProductName = ti.Product.Name,
                            Quantity = ti.Quantity,
                            ItemNote = ti.Note
                        }).ToList();

                    if (baristaItems.Any())
                    {
                        BaristaTickets.Add(new BaristaTicketModel
                        {
                            TicketId = ticket.Id,
                            TableId = ticket.TableId,
                            TableName = ticket.Table.Name,
                            TicketNote = ticket.Note,
                            Items = baristaItems,
                            OrderTime = ticket.CreatedAt // Timer'ın başlangıç noktası: Siparişin sisteme girildiği an
                        });
                    }
                }
            }
        }

        private async void ReadyButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is BaristaTicketModel ticketModel)
            {
                using (var db = new AppDbContext())
                {
                    var ticket = await db.Tickets
                        .Include(t => t.TicketItems)
                        .Include(t => t.Table)
                        .FirstOrDefaultAsync(t => t.Id == ticketModel.TicketId);

                    if (ticket != null)
                    {
                        foreach (var itemId in ticketModel.Items.Select(x => x.TicketItemId))
                        {
                            var itemToUpdate = ticket.TicketItems.FirstOrDefault(ti => ti.Id == itemId);
                            if (itemToUpdate != null)
                            {
                                itemToUpdate.Status = TicketItemStatus.Ready;
                            }
                        }

                        // Masanın statüsünü siyah yapacak olan BarReady durumuna çek
                        if (ticket.Table != null)
                        {
                            ticket.Table.Status = TableStatus.BarReady;
                        }

                        await db.SaveChangesAsync();
                    }
                }

                BaristaTickets.Remove(ticketModel);
            }
        }
    }

    // --- UI Modelleri ---
    public class BaristaTicketModel : INotifyPropertyChanged
    {
        public int TicketId { get; set; }
        public int TableId { get; set; }
        public string TableName { get; set; }
        public string TicketNote { get; set; }
        public List<BaristaItemModel> Items { get; set; }

        public DateTime OrderTime { get; set; } // Zamanlayıcı için sipariş başlangıç tarihi

        private string _timerText = "0 dk";
        public string TimerText
        {
            get => _timerText;
            set { _timerText = value; OnPropertyChanged(nameof(TimerText)); }
        }

        public void UpdateTimerText()
        {
            var diff = DateTime.Now - OrderTime;
            TimerText = $"{(int)diff.TotalMinutes} dk {(int)diff.Seconds} sn";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BaristaItemModel
    {
        public int TicketItemId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string ItemNote { get; set; }
    }
}