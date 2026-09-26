using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CafePos.Core;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Views
{
    public partial class PaymentPage : Page
    {
        private readonly int _tableId;
        private readonly string _tableName;
        private int _ticketId;

        private decimal _totalAmount = 0;
        private decimal _paidAmount = 0;
        private decimal _remainingAmount = 0;
        private decimal _selectedAmount = 0;
        private string _inputBuffer = "";

        public ObservableCollection<ConsolidatedItemModel> ConsolidatedItems { get; set; }

        public PaymentPage(int tableId, string tableName)
        {
            InitializeComponent();
            _tableId = tableId;
            _tableName = tableName;

            HeaderTextBlock.Text = $"ÖDEME: {_tableName}";
            ConsolidatedItems = new ObservableCollection<ConsolidatedItemModel>();
            ConsolidatedOrderList.ItemsSource = ConsolidatedItems;

            LoadTicketData();

            // Ekstra sayfa bazlı güvenlik (Örn: Garson buraya hiç giremesin)
            ApplyPaymentAuthorization();
        }

        private void ApplyPaymentAuthorization()
        {
            if (AppSession.CurrentUser != null && AppSession.CurrentUser.Role == UserRole.Waiter)
            {
                MessageBox.Show("Garson yetkisiyle ödeme ekranına giriş yapılamaz.", "Yetkisiz Erişim", MessageBoxButton.OK, MessageBoxImage.Stop);
                // Geri gönder
                var mainWindow = Window.GetWindow(this) as MainWindow;
                mainWindow?.ContentFrame.Navigate(new TablesPage());
            }
        }

        private void LoadTicketData()
        {
            using (var db = new AppDbContext())
            {
                // Masa ID'sine ait AÇIK olan adisyonu bul
                var ticket = db.Tickets
                    .Include(t => t.TicketItems)
                        .ThenInclude(ti => ti.Product)
                    .Include(t => t.Payments)
                    .FirstOrDefault(t => t.TableId == _tableId && t.Status == TicketStatus.Open);

                if (ticket != null)
                {
                    _ticketId = ticket.Id;

                    // 1. SOL LİSTE: Sadece ödenmemiş, ikram edilmemiş ve iade edilmemiş ürünleri listele
                    var groupedItems = ticket.TicketItems
                        .Where(ti => !ti.IsProcessed && !ti.IsTreat && !ti.IsRefunded)
                        .GroupBy(ti => ti.ProductId)
                        .Select(g => new ConsolidatedItemModel
                        {
                            ProductId = g.Key,
                            ProductName = g.First().Product.Name,
                            Quantity = g.Sum(ti => ti.Quantity),
                            UnitPrice = g.First().UnitPrice,
                            SelectedQuantity = 0 // Sayfa ilk açıldığında hiçbir ürün seçili değil
                        }).ToList();

                    ConsolidatedItems.Clear();
                    foreach (var item in groupedItems)
                    {
                        ConsolidatedItems.Add(item);
                    }

                    // 2. Önceki ödemeleri (geçmişi) sağ tarafa listele
                    var pastPayments = ticket.Payments.Select(p => new PaymentDisplayModel
                    {
                        PaymentTypeStr = p.PaymentType == PaymentType.Cash ? "Nakit" :
                                         p.PaymentType == PaymentType.CreditCard ? "Kredi Kartı" : "Yemek Kartı",
                        Amount = p.Amount
                    }).ToList();
                    PastPaymentsList.ItemsSource = pastPayments;

                    // 3. FİNANSAL HESAPLAMA 
                    _totalAmount = ticket.TicketItems
                        .Where(ti => !ti.IsTreat && !ti.IsRefunded)
                        .Sum(ti => ti.UnitPrice * ti.Quantity);

                    _totalAmount -= ticket.DiscountAmount;

                    _paidAmount = ticket.Payments.Sum(p => p.Amount);
                    _remainingAmount = _totalAmount - _paidAmount;

                    _selectedAmount = _remainingAmount;
                    _inputBuffer = "";

                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("Bu masaya ait açık bir adisyon bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateUI()
        {
            RemainingAmountText.Text = $"{_remainingAmount:N2} ₺";
            SelectedAmountText.Text = _inputBuffer != "" ? $"{_inputBuffer} ₺" : $"{_selectedAmount:N2} ₺";
        }

        private void ClearItemSelections()
        {
            foreach (var item in ConsolidatedItems) item.SelectedQuantity = 0;
        }

        private void ProductItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ConsolidatedItemModel item)
            {
                if (item.SelectedQuantity < item.Quantity)
                {
                    item.SelectedQuantity++;
                }
                else
                {
                    item.SelectedQuantity = 0;
                }

                _inputBuffer = "";
                _selectedAmount = ConsolidatedItems.Sum(x => x.SelectedQuantity * x.UnitPrice);
                UpdateUI();
            }
        }

        private void NumPad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ClearItemSelections();
                _inputBuffer += btn.Content.ToString();
                UpdateUI();
            }
        }

        private void NumPadClear_Click(object sender, RoutedEventArgs e)
        {
            ClearItemSelections();
            _inputBuffer = "";
            _selectedAmount = _remainingAmount;
            UpdateUI();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            ClearItemSelections();
            _inputBuffer = "";
            _selectedAmount = _remainingAmount;
            UpdateUI();
        }

        private void SelectHalf_Click(object sender, RoutedEventArgs e)
        {
            ClearItemSelections();
            _inputBuffer = "";
            _selectedAmount = _remainingAmount / 2;
            UpdateUI();
        }

        private void SplitByPerson_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(_inputBuffer, out int persons) && persons > 1)
            {
                ClearItemSelections();
                _selectedAmount = _remainingAmount / persons;
                _inputBuffer = "";
                UpdateUI();
            }
            else
            {
                MessageBox.Show("Lütfen numpad üzerinden geçerli bir kişi sayısı (örn: 2, 3, 4) girip tekrar 'Kişiye Böl' butonuna basınız.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyDiscount_Click(object sender, RoutedEventArgs e)
        {
            // --- GÜVENLİK KONTROLÜ: SADECE YÖNETİCİ İNDİRİM YAPABİLİR ---
            if (AppSession.CurrentUser != null && AppSession.CurrentUser.Role != UserRole.Admin)
            {
                MessageBox.Show("İndirim uygulama yetkisi sadece Yöneticilere aittir.", "Yetkisiz İşlem", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (decimal.TryParse(_inputBuffer, out decimal discountPercentage) && discountPercentage > 0 && discountPercentage <= 100)
            {
                decimal discountValue = (_remainingAmount * discountPercentage) / 100;

                using (var db = new AppDbContext())
                {
                    var ticket = db.Tickets.Find(_ticketId);
                    if (ticket != null)
                    {
                        ticket.DiscountPercentage = discountPercentage;
                        ticket.DiscountAmount += discountValue;
                        db.SaveChanges();
                    }
                }

                _inputBuffer = "";
                // Eğer XAML'da DiscountPanel yoksa hata vermemesi için null kontrolü (veya panel ismine dikkat et)
                if (DiscountPanel != null) DiscountPanel.Visibility = Visibility.Visible;
                if (DiscountAmountText != null) DiscountAmountText.Text = $"{discountValue:N2} ₺";

                LoadTicketData();
            }
            else
            {
                MessageBox.Show("Lütfen numpad üzerinden geçerli bir indirim yüzdesi (örn: 10, 20) girip tekrar 'İndirim (%)' butonuna basınız.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CashPayment_Click(object sender, RoutedEventArgs e) => ProcessPayment(PaymentType.Cash);
        private void CreditCardPayment_Click(object sender, RoutedEventArgs e) => ProcessPayment(PaymentType.CreditCard);

        private void ProcessPayment(PaymentType paymentType)
        {
            bool isItemBasedPayment = ConsolidatedItems.Any(x => x.SelectedQuantity > 0);

            if (!isItemBasedPayment && !string.IsNullOrEmpty(_inputBuffer))
            {
                if (decimal.TryParse(_inputBuffer, out decimal manualAmount)) _selectedAmount = manualAmount;
            }
            else if (!isItemBasedPayment && string.IsNullOrEmpty(_inputBuffer))
            {
                _selectedAmount = _remainingAmount;
            }

            if (_selectedAmount <= 0)
            {
                MessageBox.Show("Ödenecek tutar sıfırdan büyük olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedAmount > _remainingAmount)
            {
                MessageBox.Show("Girilen tutar kalan tutardan fazla olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new AppDbContext())
            {
                var ticket = db.Tickets.Include(t => t.Table).Include(t => t.TicketItems).FirstOrDefault(t => t.Id == _ticketId);
                if (ticket != null)
                {
                    if (isItemBasedPayment)
                    {
                        foreach (var selected in ConsolidatedItems.Where(x => x.SelectedQuantity > 0))
                        {
                            int neededToPay = selected.SelectedQuantity;
                            var matchingItems = ticket.TicketItems.Where(ti => ti.ProductId == selected.ProductId && !ti.IsProcessed).ToList();

                            foreach (var ti in matchingItems)
                            {
                                if (neededToPay <= 0) break;

                                if (ti.Quantity <= neededToPay)
                                {
                                    ti.IsProcessed = true;
                                    neededToPay -= ti.Quantity;
                                }
                                else
                                {
                                    ti.Quantity -= neededToPay;

                                    db.TicketItems.Add(new TicketItem
                                    {
                                        TicketId = ticket.Id,
                                        ProductId = ti.ProductId,
                                        Quantity = neededToPay,
                                        UnitPrice = ti.UnitPrice,
                                        IsProcessed = true,
                                        Status = ti.Status,
                                        CreatedAt = ti.CreatedAt // Kendi modelindeki CreatedAt kullanılıyor
                                    });
                                    neededToPay = 0;
                                }
                            }
                        }
                    }

                    db.Payments.Add(new Payment
                    {
                        TicketId = _ticketId,
                        Amount = _selectedAmount,
                        PaymentType = paymentType,
                        CreatedAt = DateTime.Now
                    });

                    db.SaveChanges();
                    _remainingAmount -= _selectedAmount;
                    _inputBuffer = "";

                    if (_remainingAmount <= 0)
                    {
                        ticket.Status = TicketStatus.Closed;
                        ticket.ClosedAt = DateTime.Now;
                        if (ticket.Table != null) ticket.Table.Status = TableStatus.Empty;

                        db.SaveChanges();
                        MessageBox.Show("Hesap başarıyla kapatıldı.", "İşlem Tamam", MessageBoxButton.OK, MessageBoxImage.Information);

                        var mainWindow = Window.GetWindow(this) as MainWindow;
                        mainWindow?.ContentFrame.Navigate(new TablesPage());
                    }
                    else
                    {
                        LoadTicketData();
                    }
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ContentFrame.Navigate(new TableDetailsPage(_tableId, _tableName));
        }
    }

    public class PaymentDisplayModel
    {
        public string PaymentTypeStr { get; set; }
        public decimal Amount { get; set; }
    }

    public class ConsolidatedItemModel : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        private int _selectedQuantity;
        public int SelectedQuantity
        {
            get => _selectedQuantity;
            set
            {
                _selectedQuantity = value;
                OnPropertyChanged(nameof(SelectedQuantity));
                OnPropertyChanged(nameof(DisplayQuantity));
                OnPropertyChanged(nameof(QuantityColor));
            }
        }

        public string DisplayQuantity => SelectedQuantity > 0 ? $"{SelectedQuantity} / {Quantity}" : $"{Quantity}";
        public decimal TotalPrice => Quantity * UnitPrice;

        public SolidColorBrush QuantityColor => SelectedQuantity > 0
            ? new SolidColorBrush(Color.FromRgb(33, 150, 243))
            : new SolidColorBrush(Color.FromRgb(51, 51, 51));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}