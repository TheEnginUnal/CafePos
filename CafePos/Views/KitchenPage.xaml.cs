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
    public partial class KitchenPage : UserControl
    {
        public ObservableCollection<KitchenTicketModel> KitchenTickets { get; set; }
        private DispatcherTimer _uiTimer;

        public KitchenPage()
        {
            InitializeComponent();
            KitchenTickets = new ObservableCollection<KitchenTicketModel>();
            KitchenOrdersItemsControl.ItemsSource = KitchenTickets;

            // Timer artık 10 saniyede bir tetikleniyor, böylece ekran çok daha hızlı tepki verecek
            _uiTimer = new DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromSeconds(10);
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            this.Loaded += KitchenPage_Loaded;
        }

        private async void KitchenPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadKitchenOrdersAsync();
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            foreach (var ticket in KitchenTickets)
            {
                ticket.UpdateTimerText();
            }
        }

        private async Task LoadKitchenOrdersAsync()
        {
            using (var db = new AppDbContext())
            {
                var activeTickets = await db.Tickets
                    .Include(t => t.Table)
                    .Include(t => t.TicketItems)
                        .ThenInclude(ti => ti.Product)
                    .Where(t => t.Status == TicketStatus.Open &&
                                t.TicketItems.Any(ti => ti.Status == TicketItemStatus.Preparing && ti.Product.TargetScreen == TargetScreenType.Kitchen))
                    .ToListAsync();

                KitchenTickets.Clear();

                foreach (var ticket in activeTickets)
                {
                    var kitchenItems = ticket.TicketItems
                        .Where(ti => ti.Status == TicketItemStatus.Preparing && ti.Product.TargetScreen == TargetScreenType.Kitchen)
                        .Select(ti => new KitchenItemModel
                        {
                            TicketItemId = ti.Id,
                            ProductName = ti.Product.Name,
                            Quantity = ti.Quantity,
                            ItemNote = ti.Note,
                            PreparationStartTime = ti.PreparationStartTime // Veritabanındaki saati çekiyoruz
                        }).ToList();

                    if (kitchenItems.Any())
                    {
                        // En az bir ürün fırına atılmışsa onun saatini referans alıyoruz
                        var ovenTime = kitchenItems.FirstOrDefault(x => x.PreparationStartTime != null)?.PreparationStartTime;

                        KitchenTickets.Add(new KitchenTicketModel
                        {
                            TicketId = ticket.Id,
                            TableId = ticket.TableId,
                            TableName = ticket.Table.Name,
                            TicketNote = ticket.Note,
                            Items = kitchenItems,
                            OvenStartTime = ovenTime // Modele kalıcı saati aktar
                        });
                    }
                }
            }
        }

        private async void InOvenButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is KitchenTicketModel ticketModel)
            {
                var now = DateTime.Now;

                // UI anında güncellensin diye modele veriyoruz
                ticketModel.OvenStartTime = now;

                // Veritabanına kalıcı olarak yazıyoruz
                using (var db = new AppDbContext())
                {
                    var itemIds = ticketModel.Items.Select(x => x.TicketItemId).ToList();
                    var itemsToUpdate = await db.TicketItems
                                                .Where(ti => itemIds.Contains(ti.Id))
                                                .ToListAsync();

                    foreach (var item in itemsToUpdate)
                    {
                        item.PreparationStartTime = now;
                    }

                    await db.SaveChangesAsync();
                }
            }
        }

        private async void ReadyButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is KitchenTicketModel ticketModel)
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

                        if (ticket.Table != null)
                        {
                            ticket.Table.Status = TableStatus.KitchenReady;
                        }

                        await db.SaveChangesAsync();
                    }
                }

                KitchenTickets.Remove(ticketModel);
            }
        }
    }

    // --- UI Modelleri ---
    public class KitchenTicketModel : INotifyPropertyChanged
    {
        public int TicketId { get; set; }
        public int TableId { get; set; }
        public string TableName { get; set; }
        public string TicketNote { get; set; }
        public List<KitchenItemModel> Items { get; set; }

        private DateTime? _ovenStartTime;
        public DateTime? OvenStartTime
        {
            get => _ovenStartTime;
            set
            {
                _ovenStartTime = value;
                OnPropertyChanged(nameof(OvenStartTime));
                OnPropertyChanged(nameof(IsOvenButtonEnabled));
                UpdateTimerText();
            }
        }

        public bool IsOvenButtonEnabled => !_ovenStartTime.HasValue;

        private string _timerText = "Bekliyor";
        public string TimerText
        {
            get => _timerText;
            set { _timerText = value; OnPropertyChanged(nameof(TimerText)); }
        }

        public void UpdateTimerText()
        {
            if (_ovenStartTime.HasValue)
            {
                var diff = DateTime.Now - _ovenStartTime.Value;
                // Dakika gösterimi yerine daha dinamik bir his için formati biraz süsleyebiliriz
                TimerText = $"{(int)diff.TotalMinutes} dk";
            }
            else
            {
                TimerText = "Bekliyor";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class KitchenItemModel
    {
        public int TicketItemId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string ItemNote { get; set; }
        public DateTime? PreparationStartTime { get; set; } // Model için eklendi
    }
}