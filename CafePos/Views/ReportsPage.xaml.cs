using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;
using CafePos.Models.Enums;

namespace CafePos.Views
{
    public partial class ReportsPage : Page
    {
        public ObservableCollection<CashReportTicketModel> CashTickets { get; set; }
        public ObservableCollection<PaymentDisplayModel> TicketPayments { get; set; }
        public ObservableCollection<TreatRefundModel> TicketTreats { get; set; }
        public ObservableCollection<ProductSalesModel> ProductSales { get; set; }

        public ReportsPage()
        {
            InitializeComponent();

            CashTickets = new ObservableCollection<CashReportTicketModel>();
            TicketPayments = new ObservableCollection<PaymentDisplayModel>();
            TicketTreats = new ObservableCollection<TreatRefundModel>();
            ProductSales = new ObservableCollection<ProductSalesModel>();

            TicketsListView.ItemsSource = CashTickets;
            DetailPaymentsList.ItemsSource = TicketPayments;
            DetailTreatsList.ItemsSource = TicketTreats;
            SalesListView.ItemsSource = ProductSales;

            ZReportDatePicker.SelectedDate = DateTime.Today;
            CashReportDatePicker.SelectedDate = DateTime.Today;
            SalesStartDatePicker.SelectedDate = DateTime.Today;
            SalesEndDatePicker.SelectedDate = DateTime.Today;
        }

        // ==============================================
        // 1. GÜN SONU (Z RAPORU)
        // ==============================================
        private void ZReportDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ZReportDatePicker.SelectedDate.HasValue) LoadZReport(ZReportDatePicker.SelectedDate.Value);
        }

        private void LoadZReport(DateTime date)
        {
            using (var db = new AppDbContext())
            {
                var payments = db.Payments.Where(p => p.CreatedAt.Date == date.Date).ToList();
                var tickets = db.Tickets.Include(t => t.TicketItems).ThenInclude(ti => ti.Product)
                                        .Where(t => t.CreatedAt.Date == date.Date).ToList();

                decimal cash = payments.Where(p => p.PaymentType == PaymentType.Cash).Sum(p => p.Amount);
                decimal credit = payments.Where(p => p.PaymentType == PaymentType.CreditCard).Sum(p => p.Amount);
                decimal discount = tickets.Sum(t => t.DiscountAmount);

                // DÜZELTME: İkram ve İptalleri hesaplarken sıfırlanmış UnitPrice yerine Product.Price kullanılıyor.
                var allItems = tickets.SelectMany(t => t.TicketItems).ToList();
                decimal totalTreats = allItems.Where(ti => ti.IsTreat).Sum(ti => ti.Quantity * ti.Product.Price);
                decimal totalRefunds = allItems.Where(ti => ti.IsRefunded).Sum(ti => ti.Quantity * ti.Product.Price);

                ZTotalCashText.Text = $"{cash:N2} ₺";
                ZTotalCreditCardText.Text = $"{credit:N2} ₺";
                ZTotalDiscountText.Text = $"{discount:N2} ₺";
                ZTotalRevenueText.Text = $"{(cash + credit):N2} ₺";
                ZTotalTreatsText.Text = $"{totalTreats:N2} ₺";
                ZTotalRefundsText.Text = $"{totalRefunds:N2} ₺";
            }
        }

        private void PrintZReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Z Raporu yazıcıya gönderiliyor...", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ==============================================
        // 2. KASA RAPORU
        // ==============================================
        private void CashReportDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CashReportDatePicker.SelectedDate.HasValue)
            {
                LoadCashReportTickets(CashReportDatePicker.SelectedDate.Value);
                TicketDetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadCashReportTickets(DateTime date)
        {
            using (var db = new AppDbContext())
            {
                var tickets = db.Tickets
                    .Include(t => t.Table)
                    .Include(t => t.Payments)
                    .Where(t => t.CreatedAt.Date == date.Date || t.Payments.Any(p => p.CreatedAt.Date == date.Date))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();

                CashTickets.Clear();
                foreach (var t in tickets)
                {
                    // Kapanış tarihi varsa saatini al, yoksa "-" yaz
                    string closeTimeStr = t.ClosedAt.HasValue ? t.ClosedAt.Value.ToString("HH:mm") : "-";

                    CashTickets.Add(new CashReportTicketModel
                    {
                        TicketId = t.Id,
                        TableName = t.Table?.Name ?? "Paket/Bağımsız",
                        TimeAndStatus = $"Açılış: {t.CreatedAt:HH:mm} | Kapanış: {closeTimeStr} | Durum: {(t.Status == TicketStatus.Closed ? "Kapalı" : "Açık")}"
                    });
                }
            }
        }

        private void TicketsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TicketsListView.SelectedItem is CashReportTicketModel selectedTicket)
            {
                LoadTicketDetails(selectedTicket.TicketId, selectedTicket.TableName);
            }
        }

        private void LoadTicketDetails(int ticketId, string tableName)
        {
            using (var db = new AppDbContext())
            {
                var ticket = db.Tickets
                    .Include(t => t.Payments)
                    .Include(t => t.TicketItems).ThenInclude(ti => ti.Product)
                    .FirstOrDefault(t => t.Id == ticketId);

                if (ticket != null)
                {
                    TicketDetailsPanel.Visibility = Visibility.Visible;
                    DetailTableNameText.Text = $"MASA: {tableName}";

                    TicketPayments.Clear();
                    foreach (var p in ticket.Payments)
                    {
                        TicketPayments.Add(new PaymentDisplayModel
                        {
                            PaymentTypeStr = p.PaymentType == PaymentType.Cash ? "Nakit" :
                                             p.PaymentType == PaymentType.CreditCard ? "Kredi Kartı" : "Yemek Kartı",
                            Amount = p.Amount
                        });
                    }

                    TicketTreats.Clear();
                    var treatsAndRefunds = ticket.TicketItems.Where(ti => ti.IsTreat || ti.IsRefunded).ToList();

                    if (!treatsAndRefunds.Any())
                    {
                        TicketTreats.Add(new TreatRefundModel { ItemName = "Bu adisyonda iptal veya ikram yoktur.", StatusAndStaff = "" });
                    }
                    else
                    {
                        foreach (var ti in treatsAndRefunds)
                        {
                            string actionType = ti.IsTreat ? "İkram" : "İptal/İade";
                            // DÜZELTME: Fiyat sıfırlandığı için asıl kayıp Product.Price'tan hesaplanıyor.
                            TicketTreats.Add(new TreatRefundModel
                            {
                                ItemName = $"{ti.Quantity}x {ti.Product.Name} ({actionType})",
                                StatusAndStaff = $"Tutar: {(ti.Quantity * ti.Product.Price):N2} ₺ - İşlem Yapan: Yönetici"
                            });
                        }
                    }
                }
            }
        }

        // ==============================================
        // 3. SATIŞ RAPORU
        // ==============================================
        private void SalesToday_Click(object sender, RoutedEventArgs e)
        {
            SalesStartDatePicker.SelectedDate = DateTime.Today;
            SalesEndDatePicker.SelectedDate = DateTime.Today;
        }

        private void SalesThisMonth_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            SalesStartDatePicker.SelectedDate = new DateTime(now.Year, now.Month, 1);
            SalesEndDatePicker.SelectedDate = DateTime.Today;
        }

        private void SalesLastMonth_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            SalesStartDatePicker.SelectedDate = firstDayOfThisMonth.AddMonths(-1);
            SalesEndDatePicker.SelectedDate = firstDayOfThisMonth.AddDays(-1);
        }

        private void SalesCustomDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (SalesStartDatePicker.SelectedDate.HasValue && SalesEndDatePicker.SelectedDate.HasValue)
            {
                LoadProductSales(SalesStartDatePicker.SelectedDate.Value, SalesEndDatePicker.SelectedDate.Value);
            }
        }

        private void LoadProductSales(DateTime startDate, DateTime endDate)
        {
            DateTime endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

            using (var db = new AppDbContext())
            {
                var sales = db.TicketItems
                    .Include(ti => ti.Product)
                    .Where(ti => ti.CreatedAt >= startDate && ti.CreatedAt <= endOfDay && !ti.IsRefunded && !ti.IsTreat)
                    .GroupBy(ti => ti.ProductId)
                    .Select(g => new ProductSalesModel
                    {
                        ProductName = g.First().Product.Name,
                        TotalQuantity = g.Sum(ti => ti.Quantity),
                        TotalRevenue = g.Sum(ti => ti.Quantity * ti.UnitPrice)
                    })
                    .OrderByDescending(s => s.TotalQuantity)
                    .ToList();

                ProductSales.Clear();
                foreach (var s in sales) ProductSales.Add(s);
            }
        }
    }

    public class CashReportTicketModel
    {
        public int TicketId { get; set; }
        public string TableName { get; set; }
        public string TimeAndStatus { get; set; }
    }

    public class TreatRefundModel
    {
        public string ItemName { get; set; }
        public string StatusAndStaff { get; set; }
    }

    public class ProductSalesModel
    {
        public string ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}