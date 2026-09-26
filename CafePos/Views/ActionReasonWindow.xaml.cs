using System.Linq;
using System.Windows;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;
using CafePos.Models.Enums.CafePos.Models.Enums;

namespace CafePos.Views
{
    public partial class ActionReasonWindow : Window
    {
        public bool IsConfirmed { get; private set; } = false;
        public string SelectedReason { get; private set; }
        public AppUser AuthorizedUser { get; private set; }

        private ActionType _actionType;

        public ActionReasonWindow(ActionType actionType)
        {
            InitializeComponent();
            _actionType = actionType;

            LoadReasons();
        }

        private void LoadReasons()
        {
            if (_actionType == ActionType.Treat)
            {
                TitleText.Text = "İKRAM İŞLEMİ";
                ReasonComboBox.Items.Add("Müşteri Memnuniyeti");
                ReasonComboBox.Items.Add("Personel Hakkı");
                ReasonComboBox.Items.Add("Tanıtım / Promosyon");
                ReasonComboBox.Items.Add("Yanlış Hazırlık (Telafi)");
            }
            else if (_actionType == ActionType.Refund)
            {
                TitleText.Text = "İADE / İPTAL İŞLEMİ";
                ReasonComboBox.Items.Add("Yanlış Sipariş");
                ReasonComboBox.Items.Add("Ürün Kalite Sorunu");
                ReasonComboBox.Items.Add("Müşteri Vazgeçti");
                ReasonComboBox.Items.Add("Hatalı Kayıt Girişi");
            }

            ReasonComboBox.SelectedIndex = 0;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string pin = PinCodeBox.Password;

            if (string.IsNullOrEmpty(pin))
            {
                MessageBox.Show("Lütfen yetkili PIN kodunu girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new AppDbContext())
            {
                // GÜVENLİK GÜNCELLEMESİ: Sadece 'Admin' (Yönetici) rolüne sahip ve aktif olan personeli getir
                var user = db.AppUsers.FirstOrDefault(u => u.PinCode == pin && u.IsActive && u.Role == UserRole.Admin);

                if (user == null)
                {
                    // Garson veya kasiyer pini girildiğinde verilecek net uyarı
                    MessageBox.Show("Geçersiz PIN Kodu veya Yetkisiz Kullanıcı.\n\nİkram ve İptal/İade işlemlerini yalnızca Yönetici onaylayabilir.", "Yetki Hatası", MessageBoxButton.OK, MessageBoxImage.Error);

                    PinCodeBox.Password = ""; // Şifre alanını temizle ki yönetici kendi şifresini yazabilsin
                    return;
                }

                AuthorizedUser = user;
                SelectedReason = ReasonComboBox.SelectedItem.ToString();
                IsConfirmed = true;

                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }
    }
}