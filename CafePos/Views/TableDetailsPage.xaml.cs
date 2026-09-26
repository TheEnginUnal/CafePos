using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CafePos.Core;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;
using CafePos.Models.Enums.CafePos.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Views
{
    public partial class TableDetailsPage : UserControl
    {
        private int _tableId;
        private string _tableName;

        public ObservableCollection<ProductType> MenuGroups { get; set; }
        public ObservableCollection<Product> DisplayedProducts { get; set; }
        public ObservableCollection<OrderItemModel> OrderItems { get; set; }

        public TableDetailsPage(int tableId, string tableName)
        {
            InitializeComponent();
            _tableId = tableId;
            _tableName = tableName;

            TableNameText.Text = _tableName.ToUpper();

            MenuGroups = new ObservableCollection<ProductType>();
            DisplayedProducts = new ObservableCollection<Product>();
            OrderItems = new ObservableCollection<OrderItemModel>();

            MenuGroupsItemsControl.ItemsSource = MenuGroups;
            ProductsItemsControl.ItemsSource = DisplayedProducts;
            OrderItemsList.ItemsSource = OrderItems;

            this.Loaded += TableDetailsPage_Loaded;

            // Yükleme tamamlandıktan sonra yetkileri uygula
            ApplyAuthorization();
        }

        private void TableDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMenuGroups();
            LoadExistingOpenTicket();
        }

        private void LoadMenuGroups()
        {
            using (var db = new AppDbContext())
            {
                var groups = db.ProductTypes.ToList();
                MenuGroups.Clear();
                foreach (var g in groups) MenuGroups.Add(g);

                if (MenuGroups.Any())
                {
                    LoadProductsByGroupId(MenuGroups.First().Id);
                }
            }
        }

        private void MenuGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ProductType selectedGroup)
            {
                LoadProductsByGroupId(selectedGroup.Id);
            }
        }

        private void LoadProductsByGroupId(int groupId)
        {
            using (var db = new AppDbContext())
            {
                var products = db.Products.Where(p => p.ProductTypeId == groupId && p.IsActive).ToList();
                DisplayedProducts.Clear();
                foreach (var p in products) DisplayedProducts.Add(p);
            }
        }

        private void LoadExistingOpenTicket()
        {
            using (var db = new AppDbContext())
            {
                var openTicket = db.Tickets
                                   .Include(t => t.TicketItems)
                                   .ThenInclude(ti => ti.Product)
                                   .FirstOrDefault(t => t.TableId == _tableId && t.Status == TicketStatus.Open);

                if (openTicket != null)
                {
                    OrderItems.Clear();
                    foreach (var ti in openTicket.TicketItems)
                    {
                        // Ürün ismini veritabanındaki duruma göre dinamik oluşturuyoruz (KALICI YAPI)
                        string displayName = ti.Product.Name;
                        if (ti.IsTreat) displayName = "[İKRAM] " + displayName;
                        if (ti.IsRefunded) displayName = "[İADE] " + displayName;

                        OrderItems.Add(new OrderItemModel
                        {
                            ProductId = ti.ProductId,
                            ProductName = displayName, // <-- Değişiklik burada
                            Quantity = ti.Quantity,
                            UnitPrice = ti.UnitPrice, // Ekranda 0 görünmesi için ProcessTicketItemAction içinde sıfırlıyoruz zaten
                            IsSavedInDb = true
                        });
                    }
                    UpdateTotals();
                }
            }
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Product product)
            {
                var existingItem = OrderItems.FirstOrDefault(x => x.ProductId == product.Id && !x.IsSavedInDb);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    OrderItems.Add(new OrderItemModel
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        UnitPrice = product.Price,
                        TargetScreen = product.TargetScreen.ToString(),
                        IsSavedInDb = false
                    });
                }

                OrderItemsList.Items.Refresh();
                UpdateTotals();
            }
        }

        private void UpdateTotals()
        {
            decimal total = OrderItems.Sum(x => x.TotalPrice);
            SubTotalText.Text = (total / 1.10m).ToString("C2");
            GrandTotalText.Text = total.ToString("C2");
        }

        private void IncreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OrderItemModel item)
            {
                if (item.IsSavedInDb)
                {
                    MessageBox.Show("Mevcut siparişin miktarı doğrudan değiştirilemez. İptal veya ikram işlemi uygulayınız.", "Güvenlik İhlali", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                item.Quantity++;
                OrderItemsList.Items.Refresh();
                UpdateTotals();
            }
        }

        private void DecreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OrderItemModel item)
            {
                if (item.IsSavedInDb)
                {
                    MessageBox.Show("Mevcut sipariş silinemez. İptal işlemi yapılmalıdır.", "Güvenlik İhlali", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (item.Quantity > 1) item.Quantity--;
                else OrderItems.Remove(item);

                OrderItemsList.Items.Refresh();
                UpdateTotals();
            }
        }

        // --- GÜVENLİK KONTROLLÜ İPTAL / İADE METODU ---
        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OrderItemModel item)
            {
                if (!item.IsSavedInDb)
                {
                    OrderItems.Remove(item);
                    UpdateTotals();
                    return;
                }

                if (AppSession.CurrentUser != null && AppSession.CurrentUser.Role != UserRole.Admin)
                {
                    MessageBox.Show("Kayıtlı siparişlerin iptal/iade işlemleri için Yönetici yetkisi gerekmektedir.", "Yetkisiz İşlem", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ActionReasonWindow reasonWindow = new ActionReasonWindow(ActionType.Refund);
                reasonWindow.ShowDialog();

                if (reasonWindow.IsConfirmed)
                {
                    ProcessTicketItemAction(item, ActionType.Refund, reasonWindow.SelectedReason, reasonWindow.AuthorizedUser);
                }
            }
        }

        // --- GÜVENLİK KONTROLLÜ İKRAM METODU ---
        private void TreatItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is OrderItemModel item)
            {
                if (AppSession.CurrentUser != null && AppSession.CurrentUser.Role != UserRole.Admin)
                {
                    MessageBox.Show("İkram işlemleri için Yönetici yetkisi gerekmektedir.", "Yetkisiz İşlem", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!item.IsSavedInDb)
                {
                    MessageBox.Show("Önce siparişi mutfağa göndermelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ActionReasonWindow reasonWindow = new ActionReasonWindow(ActionType.Treat);
                reasonWindow.ShowDialog();

                if (reasonWindow.IsConfirmed)
                {
                    ProcessTicketItemAction(item, ActionType.Treat, reasonWindow.SelectedReason, reasonWindow.AuthorizedUser);
                }
            }
        }

        private void ProcessTicketItemAction(OrderItemModel item, ActionType actionType, string reason, AppUser authorizedUser)
        {
            using (var db = new AppDbContext())
            {
                var activeTicket = db.Tickets.Include(t => t.TicketItems)
                                             .FirstOrDefault(t => t.TableId == _tableId && t.Status == TicketStatus.Open);

                if (activeTicket != null)
                {
                    var ticketItem = activeTicket.TicketItems.FirstOrDefault(ti => ti.ProductId == item.ProductId && !ti.IsProcessed);

                    if (ticketItem != null)
                    {
                        var actionLog = new TicketActionLog
                        {
                            TicketId = activeTicket.Id,
                            TicketItemId = ticketItem.Id,
                            AppUserId = authorizedUser.Id,
                            ActionType = actionType,
                            ReasonCode = reason,
                            OriginalValue = ticketItem.UnitPrice * ticketItem.Quantity,
                            AdjustmentValue = ticketItem.UnitPrice * ticketItem.Quantity
                        };
                        db.TicketActionLogs.Add(actionLog);

                        ticketItem.IsProcessed = true;
                        ticketItem.FinalPrice = 0;

                        if (actionType == ActionType.Treat)
                        {
                            ticketItem.IsTreat = true;
                            item.ProductName = "[İKRAM] " + item.ProductName.Replace("[İKRAM] ", "").Replace("[İADE] ", "");
                        }
                        else if (actionType == ActionType.Refund)
                        {
                            ticketItem.IsRefunded = true;
                            item.ProductName = "[İADE] " + item.ProductName.Replace("[İKRAM] ", "").Replace("[İADE] ", "");
                        }

                        db.SaveChanges();

                        item.UnitPrice = 0;
                        OrderItemsList.Items.Refresh();
                        UpdateTotals();

                        MessageBox.Show($"{actionType} işlemi {authorizedUser.FullName} yetkisiyle onaylandı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void SendOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var newItems = OrderItems.Where(x => !x.IsSavedInDb).ToList();

            if (newItems.Count == 0)
            {
                MessageBox.Show("Gönderilecek yeni sipariş bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string orderNote = OrderNoteTextBox.Text.Trim();
            int activeTicketId = 0;

            using (var db = new AppDbContext())
            {
                var activeTicket = db.Tickets.FirstOrDefault(t => t.TableId == _tableId && t.Status == TicketStatus.Open);

                if (activeTicket == null)
                {
                    activeTicket = new Ticket
                    {
                        TableId = _tableId,
                        Status = TicketStatus.Open,
                        CreatedAt = DateTime.Now,
                        Note = orderNote,
                        RoundingAmount = 0,
                        DiscountAmount = 0,
                        DiscountPercentage = 0
                    };
                    db.Tickets.Add(activeTicket);
                }
                else
                {
                    if (!string.IsNullOrEmpty(orderNote))
                    {
                        activeTicket.Note = string.IsNullOrEmpty(orderNote) ? null : orderNote;
                    }
                }

                foreach (var item in newItems)
                {
                    var ticketItem = new TicketItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Status = TicketItemStatus.Preparing,
                        IsTreat = false,
                        IsRefunded = false,
                    };

                    activeTicket.TicketItems.Add(ticketItem);
                    item.IsSavedInDb = true;
                }

                var table = db.Tables.Find(_tableId);
                if (table != null)
                {
                    if (table.Status != TableStatus.Waiting)
                    {
                        table.Status = TableStatus.Waiting;
                    }
                }

                db.SaveChanges();
                activeTicketId = activeTicket.Id;
            }

            var kitchenItems = newItems.Where(x => x.TargetScreen == TargetScreenType.Kitchen.ToString()).ToList();
            var baristaItems = newItems.Where(x => x.TargetScreen == TargetScreenType.Barista.ToString()).ToList();

            MessageBox.Show($"İlave siparişler başarıyla eklendi.\nMutfağa giden ürün sayısı: {kitchenItems.Count}\nBaristaya giden ürün sayısı: {baristaItems.Count}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

            OrderNoteTextBox.Text = string.Empty;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ContentFrame.Navigate(new TablesPage());
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.ContentFrame != null)
            {
                mainWindow.ContentFrame.Navigate(new PaymentPage(_tableId, _tableName));
            }
        }

        private void PrintTicketButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fiş yazdırma işlemi başlatıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var unsavedItems = OrderItems.Where(x => !x.IsSavedInDb).ToList();

            if (unsavedItems.Count > 0)
            {
                foreach (var item in unsavedItems)
                {
                    OrderItems.Remove(item);
                }
                UpdateTotals();
            }
            else
            {
                MessageBox.Show("İptal edilecek yeni sipariş yok. Kayıtlı siparişleri iptal/iade etmek için yönetici onayı gerekir.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;

                if (mainWindow != null && mainWindow.ContentFrame != null)
                {
                    mainWindow.ContentFrame.Navigate(new TablesPage());
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Masalar sayfasına dönülürken bir hata oluştu:\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyAuthorization()
        {
            if (AppSession.CurrentUser == null) return;

            switch (AppSession.CurrentUser.Role)
            {
                case UserRole.Waiter:
                    if (CheckoutButton != null) CheckoutButton.Visibility = Visibility.Collapsed;
                    if (CancelButton != null) CancelButton.Visibility = Visibility.Collapsed;
                    break;

                case UserRole.Cashier:
                    if (CheckoutButton != null) CheckoutButton.Visibility = Visibility.Visible;
                    if (CancelButton != null) CancelButton.Visibility = Visibility.Collapsed;
                    break;

                case UserRole.Admin:
                    if (CheckoutButton != null) CheckoutButton.Visibility = Visibility.Visible;
                    if (CancelButton != null) CancelButton.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    public class OrderItemModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public string TargetScreen { get; set; }
        public bool IsSavedInDb { get; set; }
    }
}