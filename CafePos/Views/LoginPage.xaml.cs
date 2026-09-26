using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CafePos.Data;
using CafePos.Core;

namespace CafePos.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void NumPad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (PinPasswordBox.Password.Length < 4)
                {
                    PinPasswordBox.Password += btn.Content.ToString();

                    // 4 Hane girildiği an otomatik giriş tetikle
                    if (PinPasswordBox.Password.Length == 4)
                    {
                        AttemptLogin();
                    }
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (PinPasswordBox.Password.Length > 0)
            {
                PinPasswordBox.Password = PinPasswordBox.Password.Substring(0, PinPasswordBox.Password.Length - 1);
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            PinPasswordBox.Password = "";
        }

        private void AttemptLogin()
        {
            string enteredPin = PinPasswordBox.Password;

            using (var db = new AppDbContext())
            {
                var user = db.AppUsers.FirstOrDefault(u => u.PinCode == enteredPin && u.IsActive);

                if (user != null)
                {
                    // Başarılı Giriş: Oturumu başlat
                    AppSession.CurrentUser = user;

                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.ApplyAuthorizationAndShowMenu(); // Yetkileri uygula ve menüyü aç
                        mainWindow.ContentFrame.Navigate(new TablesPage());
                    }
                }
                else
                {
                    MessageBox.Show("Hatalı PIN Kodu veya pasif personel girişi.", "Giriş Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                    PinPasswordBox.Password = ""; // Ekranı temizle
                }
            }
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Sistemi tamamen kapatmak istediğinize emin misiniz?",
                                         "Sistemi Kapat",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}