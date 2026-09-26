using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using CafePos.Services;
using CafePos.Views;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;
using CafePos.Core; // AppSession için eklendi

namespace CafePos
{
    public partial class MainWindow : Window
    {
        private bool _isMenuExpanded = true;
        private bool _isReportsMenuOpen = false;
        private bool _isSettingsMenuOpen = false;

        private SignalRClientService _signalRService;

        public MainWindow()
        {
            InitializeComponent();

            // Program açıldığında direkt Login ekranını yükle
            ContentFrame.Navigate(new LoginPage());

            CreateDefaultAdminUser();
            InitializeSignalR();

            // 1. UYGULAMA İLK AÇILDIĞINDA MENÜYÜ GİZLE (Genişliği 0 yap)
            MenuColumn.Width = new GridLength(0);
        }

        // --- YETKİLENDİRME VE MENÜYÜ GÖSTERME METODU ---
        public void ApplyAuthorizationAndShowMenu()
        {
            if (AppSession.CurrentUser == null) return;

            // Menüyü tekrar görünür yap
            MenuColumn.Width = new GridLength(280);
            _isMenuExpanded = true;
            MenuTitle.Visibility = Visibility.Visible;

            // YETKİ KONTROLLERİ
            // Not: MainWindow.xaml dosyasında Raporlar butonuna x:Name="ReportsMenuButton" 
            // ve Ayarlar butonuna x:Name="SettingsMenuButton" vermelisin.
            if (AppSession.CurrentUser.Role == UserRole.Waiter)
            {
                ReportsMenuButton.Visibility = Visibility.Collapsed;
                SettingsMenuButton.Visibility = Visibility.Collapsed;
                SettingsSubMenu.Visibility = Visibility.Collapsed;
            }
            else if (AppSession.CurrentUser.Role == UserRole.Cashier)
            {
                ReportsMenuButton.Visibility = Visibility.Collapsed;
                SettingsMenuButton.Visibility = Visibility.Collapsed;
                SettingsSubMenu.Visibility = Visibility.Collapsed;
            }
            else if (AppSession.CurrentUser.Role == UserRole.Admin)
            {
                ReportsMenuButton.Visibility = Visibility.Visible;
                SettingsMenuButton.Visibility = Visibility.Visible;
            }
        }

        private void CreateDefaultAdminUser()
        {
            using (var db = new AppDbContext())
            {
                if (!db.AppUsers.Any())
                {
                    var adminUser = new AppUser
                    {
                        FullName = "Sistem Yöneticisi",
                        UserName = "admin",
                        PinCode = "1234",
                        Role = UserRole.Admin,
                        IsActive = true
                    };

                    db.AppUsers.Add(adminUser);
                    db.SaveChanges();
                }
            }
        }

        private async void InitializeSignalR()
        {
            try
            {
                // SignalR kodların...
            }
            catch (Exception ex)
            {
            }
        }

        private void SignalRService_OnOrderUpdated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ContentFrame.Content is TablesPage tablesPage)
                {
                    tablesPage.RefreshTables();
                }
            });
        }

        private void SignalRService_OnTableStatusChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ContentFrame.Content is TablesPage tablesPage)
                {
                    tablesPage.RefreshTables();
                }
            });
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string tag = clickedButton?.Tag?.ToString();

            switch (tag)
            {
                case "SummaryPage":
                    // ContentFrame.Navigate(new SummaryPage());
                    break;
                case "TablesPage":
                    ContentFrame.Navigate(new TablesPage());
                    break;
                case "KitchenPage":
                    ContentFrame.Navigate(new KitchenPage());
                    break;
                case "BaristaPage":
                    ContentFrame.Navigate(new BaristaPage());
                    break;
                case "StockPage":
                    // ContentFrame.Navigate(new StockPage());
                    break;
                case "ReportPage":
                    ContentFrame.Navigate(new ReportsPage());
                    break;
                case "UserSettingsPage":
                    ContentFrame.Navigate(new UserSettingsPage());
                    break;
                case "ProductSettingsPage":
                    ContentFrame.Navigate(new ProductSettingsPage());
                    break;
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMenuExpanded)
            {
                AnimateMenuWidth(280, 80);
                MenuTitle.Visibility = Visibility.Collapsed;
                SettingsSubMenu.Visibility = Visibility.Collapsed;
                _isReportsMenuOpen = false;
                _isSettingsMenuOpen = false;
                _isMenuExpanded = false;
            }
            else
            {
                AnimateMenuWidth(80, 280);
                MenuTitle.Visibility = Visibility.Visible;
                _isMenuExpanded = true;
            }
        }

        private void AnimateMenuWidth(double from, double to)
        {
            GridLengthAnimation animation = new GridLengthAnimation
            {
                From = new GridLength(from),
                To = new GridLength(to),
                Duration = TimeSpan.FromMilliseconds(200)
            };
            MenuColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation);
        }

        private void ToggleReportsMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMenuExpanded) HamburgerButton_Click(null, null);

            _isReportsMenuOpen = !_isReportsMenuOpen;

            if (_isReportsMenuOpen)
            {
                SettingsSubMenu.Visibility = Visibility.Collapsed;
                _isSettingsMenuOpen = false;
            }
        }

        private void ToggleSettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMenuExpanded) HamburgerButton_Click(null, null);

            _isSettingsMenuOpen = !_isSettingsMenuOpen;
            SettingsSubMenu.Visibility = _isSettingsMenuOpen ? Visibility.Visible : Visibility.Collapsed;

            if (_isSettingsMenuOpen)
            {
                _isReportsMenuOpen = false;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Oturumu kapatıp giriş ekranına dönmek istediğinize emin misiniz?",
                                         "Çıkış Yap",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 1. Aktif oturumu temizle
                AppSession.Logout();

                // 2. Sol menüyü tamamen gizle ve durumları sıfırla
                MenuColumn.Width = new GridLength(0);
                _isMenuExpanded = true;
                _isReportsMenuOpen = false;
                _isSettingsMenuOpen = false;
                SettingsSubMenu.Visibility = Visibility.Collapsed;

                // 3. Login (Giriş) ekranına yönlendir
                ContentFrame.Navigate(new LoginPage());
            }
        }
    }

    public class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);
        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

        public static readonly DependencyProperty FromProperty = DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));
        public GridLength From
        {
            get { return (GridLength)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public static readonly DependencyProperty ToProperty = DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));
        public GridLength To
        {
            get { return (GridLength)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = ((GridLength)GetValue(FromProperty)).Value;
            double toVal = ((GridLength)GetValue(ToProperty)).Value;

            if (fromVal > toVal)
            {
                return new GridLength((1 - animationClock.CurrentProgress.Value) * (fromVal - toVal) + toVal, GridUnitType.Pixel);
            }
            else
            {
                return new GridLength(animationClock.CurrentProgress.Value * (toVal - fromVal) + fromVal, GridUnitType.Pixel);
            }
        }
    }
}