using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CafePos.Data;
using CafePos.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Views
{
    public partial class ChangeTableWindow : Window
    {
        private int _sourceTableId;
        private string _sourceTableName;

        public bool IsActionCompleted { get; private set; } = false;
        public ObservableCollection<TargetTableModel> TargetTables { get; set; }

        public ChangeTableWindow(int sourceTableId, string sourceTableName)
        {
            InitializeComponent();
            _sourceTableId = sourceTableId;
            _sourceTableName = sourceTableName;

            TitleText.Text = $"{_sourceTableName} MASASINI AKTAR / BİRLEŞTİR";

            TargetTables = new ObservableCollection<TargetTableModel>();
            TablesItemsControl.ItemsSource = TargetTables;

            LoadAvailableTables();
        }

        private void LoadAvailableTables()
        {
            using (var db = new AppDbContext())
            {
                // Şu anki masa hariç tüm masaları getir
                var tables = db.Tables.Where(t => t.Id != _sourceTableId).ToList();

                TargetTables.Clear();
                foreach (var table in tables)
                {
                    // Masa durumuna göre renk ve metin belirle
                    SolidColorBrush color;
                    string statusText;
                    bool isSelectable = true;

                    switch (table.Status)
                    {
                        case TableStatus.Empty:
                            color = new SolidColorBrush(Color.FromRgb(96, 125, 139)); // Gri (Boş)
                            statusText = "Boş";
                            break;
                        case TableStatus.Waiting:
                        case TableStatus.KitchenReady:
                        case TableStatus.BarReady:
                            color = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Turuncu (Dolu)
                            statusText = "Açık Adisyon";
                            break;
                        case TableStatus.Delivered: // <-- KISITLAMANIN KALKTIĞI YER (YEŞİL MASALAR)
                            color = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Yeşil (Teslim Edildi)
                            statusText = "Teslim Edildi";
                            break;
                        default:
                            color = new SolidColorBrush(Color.FromRgb(229, 57, 53)); // Kırmızı vs
                            statusText = "Seçilemez";
                            isSelectable = false;
                            break;
                    }

                    if (isSelectable)
                    {
                        TargetTables.Add(new TargetTableModel
                        {
                            TableId = table.Id,
                            TableName = table.Name,
                            StatusText = statusText,
                            StatusColor = color,
                            IsTargetEmpty = (table.Status == TableStatus.Empty)
                        });
                    }
                }
            }
        }

        private void TargetTable_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is TargetTableModel targetTable)
            {
                string actionName = targetTable.IsTargetEmpty ? "TAŞINACAK" : "BİRLEŞTİRİLECEK";

                var result = MessageBox.Show(
                    $"{_sourceTableName} masasındaki tüm siparişler {targetTable.TableName} masasına {actionName}.\n\nOnaylıyor musunuz?",
                    "Masa Aktarım Onayı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    PerformTableTransfer(targetTable.TableId, targetTable.IsTargetEmpty);
                }
            }
        }

        private void PerformTableTransfer(int targetTableId, bool isTargetEmpty)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // 1. Kaynak Masanın Açık Adisyonunu Bul
                    var sourceTicket = db.Tickets.Include(t => t.TicketItems)
                                                 .Include(t => t.Payments)
                                                 .FirstOrDefault(t => t.TableId == _sourceTableId && t.Status == TicketStatus.Open);

                    if (sourceTicket == null)
                    {
                        MessageBox.Show("Kaynak masada açık bir adisyon bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 2. TAŞIMA (Transfer): Hedef masa boş ise
                    if (isTargetEmpty)
                    {
                        // Sadece adisyonun ait olduğu masa ID'sini güncelliyoruz
                        sourceTicket.TableId = targetTableId;

                        // Kaynak masayı Boş, Hedef masayı Dolu yap
                        var sourceTable = db.Tables.Find(_sourceTableId);
                        if (sourceTable != null) sourceTable.Status = TableStatus.Empty;

                        var targetTable = db.Tables.Find(targetTableId);
                        if (targetTable != null) targetTable.Status = TableStatus.Waiting;

                        db.SaveChanges();
                    }
                    // 3. BİRLEŞTİRME (Merge): Hedef masanın zaten açık adisyonu varsa
                    else
                    {
                        var targetTicket = db.Tickets.Include(t => t.TicketItems)
                                                     .Include(t => t.Payments)
                                                     .FirstOrDefault(t => t.TableId == targetTableId && t.Status == TicketStatus.Open);

                        if (targetTicket == null)
                        {
                            MessageBox.Show("Hedef masada açık adisyon bulunamadı (Birleştirme iptal edildi).", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Kaynaktaki tüm ürünleri hedefe aktar
                        foreach (var item in sourceTicket.TicketItems.ToList())
                        {
                            item.TicketId = targetTicket.Id; // Ürünün bağlı olduğu adisyonu değiştir
                        }

                        // Kaynaktaki tüm ödemeleri hedefe aktar
                        foreach (var payment in sourceTicket.Payments.ToList())
                        {
                            payment.TicketId = targetTicket.Id;
                        }

                        // Not ve indirimleri birleştir
                        if (!string.IsNullOrEmpty(sourceTicket.Note))
                        {
                            targetTicket.Note = string.IsNullOrEmpty(targetTicket.Note)
                                ? sourceTicket.Note
                                : $"{targetTicket.Note} | {sourceTicket.Note}";
                        }

                        targetTicket.DiscountAmount += sourceTicket.DiscountAmount;

                        // Kaynak adisyonu Kapat/İptal Et (Sıfırlandığı için)
                        sourceTicket.Status = TicketStatus.Closed; // veya "Merged" gibi bir özel durumunuz varsa onu kullanın

                        var sourceTable = db.Tables.Find(_sourceTableId);
                        if (sourceTable != null) sourceTable.Status = TableStatus.Empty;

                        db.SaveChanges();
                    }

                    IsActionCompleted = true;
                    MessageBox.Show("Masa aktarım / birleştirme işlemi başarıyla tamamlandı.", "İşlem Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Aktarım sırasında bir hata oluştu:\n\n{ex.Message}", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class TargetTableModel
    {
        public int TableId { get; set; }
        public string TableName { get; set; }
        public string StatusText { get; set; }
        public SolidColorBrush StatusColor { get; set; }
        public bool IsTargetEmpty { get; set; }
    }
}