using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;

namespace CafePos.Views
{
    public partial class UserSettingsPage : Page
    {
        public ObservableCollection<AppUser> UsersList { get; set; }
        private AppUser _selectedUser;

        public UserSettingsPage()
        {
            InitializeComponent();
            UsersList = new ObservableCollection<AppUser>();
            UsersListView.ItemsSource = UsersList;

            LoadUsers();
        }

        private void LoadUsers()
        {
            using (var db = new AppDbContext())
            {
                var users = db.AppUsers.ToList();
                UsersList.Clear();
                foreach (var user in users)
                {
                    UsersList.Add(user);
                }
            }
        }

        private void UsersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersListView.SelectedItem is AppUser user)
            {
                _selectedUser = user;
                FormTitle.Text = "Personeli Düzenle";

                FullNameInput.Text = user.FullName;
                UserNameInput.Text = user.UserName;
                PinCodeInput.Text = user.PinCode;
                IsActiveCheckBox.IsChecked = user.IsActive;

                // Role seçimini ComboBox'a yansıt
                foreach (ComboBoxItem item in RoleComboBox.Items)
                {
                    if (item.Tag.ToString() == ((int)user.Role).ToString())
                    {
                        RoleComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void SaveUser_Click(object sender, RoutedEventArgs e)
        {
            // Validasyonlar
            if (string.IsNullOrWhiteSpace(FullNameInput.Text) || string.IsNullOrWhiteSpace(UserNameInput.Text) || string.IsNullOrWhiteSpace(PinCodeInput.Text))
            {
                MessageBox.Show("Lütfen tüm alanları doldurunuz.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PinCodeInput.Text.Length < 4 || !PinCodeInput.Text.All(char.IsDigit))
            {
                MessageBox.Show("PIN Kodu sadece 4 haneli rakamlardan oluşmalıdır.", "Geçersiz PIN", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir rol seçiniz.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new AppDbContext())
            {
                // PIN kodunun başka birinde olup olmadığını kontrol et
                bool pinExists = db.AppUsers.Any(u => u.PinCode == PinCodeInput.Text && (_selectedUser == null || u.Id != _selectedUser.Id));
                if (pinExists)
                {
                    MessageBox.Show("Bu PIN Kodu başka bir personel tarafından kullanılmaktadır. Lütfen farklı bir PIN belirleyin.", "Çakışma", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int selectedRoleInt = int.Parse(((ComboBoxItem)RoleComboBox.SelectedItem).Tag.ToString());

                if (_selectedUser == null)
                {
                    // Yeni Ekleme
                    var newUser = new AppUser
                    {
                        FullName = FullNameInput.Text,
                        UserName = UserNameInput.Text,
                        PinCode = PinCodeInput.Text,
                        Role = (UserRole)selectedRoleInt,
                        IsActive = IsActiveCheckBox.IsChecked ?? true
                    };
                    db.AppUsers.Add(newUser);
                }
                else
                {
                    // Güncelleme
                    var userToUpdate = db.AppUsers.Find(_selectedUser.Id);
                    if (userToUpdate != null)
                    {
                        userToUpdate.FullName = FullNameInput.Text;
                        userToUpdate.UserName = UserNameInput.Text;
                        userToUpdate.PinCode = PinCodeInput.Text;
                        userToUpdate.Role = (UserRole)selectedRoleInt;
                        userToUpdate.IsActive = IsActiveCheckBox.IsChecked ?? true;
                    }
                }

                db.SaveChanges();
            }

            MessageBox.Show("Personel bilgileri başarıyla kaydedildi.", "İşlem Tamam", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearForm();
            LoadUsers();
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _selectedUser = null;
            FormTitle.Text = "Yeni Personel Ekle";
            FullNameInput.Text = "";
            UserNameInput.Text = "";
            PinCodeInput.Text = "";
            RoleComboBox.SelectedItem = null;
            IsActiveCheckBox.IsChecked = true;
            UsersListView.SelectedItem = null;
        }
    }
}