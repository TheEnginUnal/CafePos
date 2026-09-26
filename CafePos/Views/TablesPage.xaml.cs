using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CafePos.Models.Entities;
using CafePos.Models.Enums;
using CafePos.Repositories;

namespace CafePos.Views
{
    public partial class TablesPage : UserControl
    {
        // Masaları arayüzde göstermek için kullanacağımız liste (ObservableCollection ekranı otomatik günceller)
        public ObservableCollection<TableDisplayModel> TablesList { get; set; }

        private GenericRepository<Table> _tableRepository;
        private TicketRepository _ticketRepository;

        public TablesPage()
        {
            InitializeComponent();

            _tableRepository = new GenericRepository<Table>();
            _ticketRepository = new TicketRepository();
            TablesList = new ObservableCollection<TableDisplayModel>();

            // XAML tarafındaki ItemsSource bağlaması için DataContext'i kendisi yapıyoruz
            this.DataContext = this;

            // Sayfa yüklendiğinde masaları veritabanından çek
            this.Loaded += TablesPage_Loaded;
        }

        private async void TablesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTablesAsync();
        }

        private async Task LoadTablesAsync()
        {
            TablesList.Clear();
            var allTables = await _tableRepository.GetAllAsync();

            // Eğer masa sayısı 35'ten azsa eksikleri tamamla
            if (allTables.Count < 35)
            {
                int currentCount = allTables.Count;
                for (int i = currentCount + 1; i <= 35; i++)
                {
                    await _tableRepository.AddAsync(new Table { Name = $"M-{i}", Status = TableStatus.Empty });
                }
                // Eksikler eklendikten sonra listeyi veritabanından tekrar çek
                allTables = await _tableRepository.GetAllAsync();
            }

            foreach (var table in allTables)
            {
                TablesList.Add(new TableDisplayModel
                {
                    Id = table.Id,
                    Name = table.Name,
                    Status = table.Status,
                    StatusColor = GetColorForStatus(table.Status) // Rengi kurallarına göre belirler
                });
            }

            TablesItemsControl.ItemsSource = TablesList;
        }

        // Tıklanan masaya göre işlem yapma
        // Tıklanan masaya göre işlem yapma
        private void TableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Tıklanan öğeyi ve modeli güvenli bir şekilde yakala
                if (sender is FrameworkElement element && element.DataContext is TableDisplayModel clickedTable)
                {
                    // 2. Application.Current yerine doğrudan bu sayfanın içinde bulunduğu Pencereyi (Window) bul
                    var mainWindow = Window.GetWindow(this) as MainWindow;

                    if (mainWindow != null && mainWindow.ContentFrame != null)
                    {
                        // 3. Geçişi başlat
                        mainWindow.ContentFrame.Navigate(new TableDetailsPage(clickedTable.Id, clickedTable.Name));
                    }
                    else
                    {
                        MessageBox.Show("Hata: Ana pencere (MainWindow) veya ContentFrame bulunamadı. Yönlendirme yapılamıyor.", "Sistem Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Eğer TableDetailsPage açılırken veritabanı vb. bir hata patlıyorsa bunu ekranda gösterecek
                MessageBox.Show($"Adisyon sayfasına geçiş yapılırken bir hata oluştu:\n\n{ex.Message}\n\nİç Hata: {ex.InnerException?.Message}", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Masanın durumuna göre senin belirlediğin renkleri (Brush) döndüren metod
        private SolidColorBrush GetColorForStatus(TableStatus status)
        {
            return status switch
            {
                TableStatus.Empty => new SolidColorBrush(Colors.Gray),                                          // Boş
                TableStatus.Waiting => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500")), // Turuncu: Sipariş girildi, bekliyor
                TableStatus.KitchenReady => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E81123")), // Kırmızı: Mutfak hazırladı
                TableStatus.BarReady => new SolidColorBrush(Colors.Black),                                      // Siyah: Barista hazırladı
                TableStatus.Delivered => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10")),    // Yeşil: Teslim edildi
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        // Dışarıdan (MainWindow'dan veya SignalR'dan) masaları yenilemek için kullanılacak metod
        public async void RefreshTables()
        {
            await LoadTablesAsync();
        }
    }

    // Arayüzde (XAML) daha rahat göstermek için oluşturduğumuz taşıyıcı model
    public class TableDisplayModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TableStatus Status { get; set; }
        public SolidColorBrush StatusColor { get; set; }
    }
}