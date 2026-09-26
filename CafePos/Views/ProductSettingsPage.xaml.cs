using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;
using CafePos.Models.Entities;
using CafePos.Models.Enums;

namespace CafePos.Views
{
    public partial class ProductSettingsPage : UserControl
    {
        public ObservableCollection<Category> SettingsCategories { get; set; }
        public ObservableCollection<ProductType> SettingsProductTypes { get; set; }
        public ObservableCollection<ProductDisplayModel> SettingsProducts { get; set; }

        // Düzenleme modunda olup olmadığımızı takip eden ID'ler
        private int? _editingCategoryId = null;
        private int? _editingProductTypeId = null;
        private int? _editingProductId = null;

        public ProductSettingsPage()
        {
            InitializeComponent();

            SettingsCategories = new ObservableCollection<Category>();
            SettingsProductTypes = new ObservableCollection<ProductType>();
            SettingsProducts = new ObservableCollection<ProductDisplayModel>();

            CategoriesListView.ItemsSource = SettingsCategories;
            CategoryCombo.ItemsSource = SettingsCategories;

            ProductTypesListView.ItemsSource = SettingsProductTypes;
            ProductTypeCombo.ItemsSource = SettingsProductTypes;

            ProductsListView.ItemsSource = SettingsProducts;

            this.Loaded += ProductSettingsPage_Loaded;
        }

        private void ProductSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            using (var db = new AppDbContext())
            {
                SettingsCategories.Clear();
                foreach (var cat in db.Categories.ToList()) SettingsCategories.Add(cat);

                SettingsProductTypes.Clear();
                foreach (var pt in db.ProductTypes.ToList()) SettingsProductTypes.Add(pt);

                SettingsProducts.Clear();
                var products = db.Products.Include(p => p.Category).Include(p => p.ProductType).ToList();
                foreach (var prod in products)
                {
                    SettingsProducts.Add(new ProductDisplayModel
                    {
                        Id = prod.Id,
                        CategoryId = prod.CategoryId,
                        CategoryName = prod.Category?.Name,
                        ProductTypeId = prod.ProductTypeId,
                        ProductTypeName = prod.ProductType?.Name,
                        Name = prod.Name,
                        Price = prod.Price,
                        TargetScreen = prod.TargetScreen.ToString(),
                        TargetScreenText = GetTargetScreenDisplayName(prod.TargetScreen),
                        IsStockTracked = prod.IsStockTracked
                    });
                }
            }
        }

        // ================= KATEGORİ İŞLEMLERİ =================
        private void SaveCategory_Click(object sender, RoutedEventArgs e)
        {
            string catName = CatNameText.Text.Trim();
            if (string.IsNullOrEmpty(catName)) return;

            using (var db = new AppDbContext())
            {
                if (_editingCategoryId == null) // EKLEME
                {
                    db.Categories.Add(new Category { Name = catName });
                }
                else // GÜNCELLEME
                {
                    var cat = db.Categories.Find(_editingCategoryId);
                    if (cat != null) cat.Name = catName;
                }
                db.SaveChanges();
            }
            LoadDataFromDatabase();
            CancelCategory_Click(null, null); // Formu temizle
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Category cat)
            {
                _editingCategoryId = cat.Id;
                CatNameText.Text = cat.Name;
                BtnSaveCategory.Content = "GÜNCELLE";
                BtnSaveCategory.Background = new SolidColorBrush(Colors.Orange);
                BtnCancelCategory.Visibility = Visibility.Visible;
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Category cat)
            {
                using (var db = new AppDbContext())
                {
                    if (db.Products.Any(p => p.CategoryId == cat.Id))
                    {
                        MessageBox.Show("Bu kategoriye ait ürünler var!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    db.Categories.Remove(db.Categories.Find(cat.Id));
                    db.SaveChanges();
                }
                LoadDataFromDatabase();
            }
        }

        private void CancelCategory_Click(object sender, RoutedEventArgs e)
        {
            _editingCategoryId = null;
            CatNameText.Clear();
            BtnSaveCategory.Content = "EKLE";
            BtnSaveCategory.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D7"));
            BtnCancelCategory.Visibility = Visibility.Collapsed;
        }

        // ================= MENÜ GRUBU İŞLEMLERİ =================
        private void SaveProductType_Click(object sender, RoutedEventArgs e)
        {
            string ptName = ProductTypeNameText.Text.Trim();
            if (string.IsNullOrEmpty(ptName)) return;

            using (var db = new AppDbContext())
            {
                if (_editingProductTypeId == null)
                {
                    db.ProductTypes.Add(new ProductType { Name = ptName });
                }
                else
                {
                    var pt = db.ProductTypes.Find(_editingProductTypeId);
                    if (pt != null) pt.Name = ptName;
                }
                db.SaveChanges();
            }
            LoadDataFromDatabase();
            CancelProductType_Click(null, null);
        }

        private void EditProductType_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ProductType pt)
            {
                _editingProductTypeId = pt.Id;
                ProductTypeNameText.Text = pt.Name;
                BtnSaveProductType.Content = "GÜNCELLE";
                BtnSaveProductType.Background = new SolidColorBrush(Colors.Orange);
                BtnCancelProductType.Visibility = Visibility.Visible;
            }
        }

        private void DeleteProductType_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ProductType pt)
            {
                using (var db = new AppDbContext())
                {
                    if (db.Products.Any(p => p.ProductTypeId == pt.Id))
                    {
                        MessageBox.Show("Bu menü grubuna ait ürünler var!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    db.ProductTypes.Remove(db.ProductTypes.Find(pt.Id));
                    db.SaveChanges();
                }
                LoadDataFromDatabase();
            }
        }

        private void CancelProductType_Click(object sender, RoutedEventArgs e)
        {
            _editingProductTypeId = null;
            ProductTypeNameText.Clear();
            BtnSaveProductType.Content = "EKLE";
            BtnSaveProductType.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D7"));
            BtnCancelProductType.Visibility = Visibility.Collapsed;
        }

        // ================= ÜRÜN İŞLEMLERİ =================
        private void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryCombo.SelectedValue == null || ProductTypeCombo.SelectedValue == null)
            {
                MessageBox.Show("Lütfen Kategori ve Menü Grubunu seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ProductPriceText.Text.Trim(), out decimal price))
            {
                MessageBox.Show("Geçerli bir fiyat girin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var db = new AppDbContext())
            {
                ComboBoxItem selectedTarget = (ComboBoxItem)TargetScreenCombo.SelectedItem;
                TargetScreenType screenType = Enum.Parse<TargetScreenType>(selectedTarget?.Tag?.ToString() ?? "None");

                if (_editingProductId == null) // YENİ EKLE
                {
                    db.Products.Add(new Product
                    {
                        CategoryId = (int)CategoryCombo.SelectedValue,
                        ProductTypeId = (int)ProductTypeCombo.SelectedValue,
                        Name = ProductNameText.Text.Trim(),
                        Price = price,
                        TargetScreen = screenType,
                        IsStockTracked = IsStockTrackedCheck.IsChecked ?? false,
                        IsActive = true
                    });
                }
                else // GÜNCELLE
                {
                    var prod = db.Products.Find(_editingProductId);
                    if (prod != null)
                    {
                        prod.CategoryId = (int)CategoryCombo.SelectedValue;
                        prod.ProductTypeId = (int)ProductTypeCombo.SelectedValue;
                        prod.Name = ProductNameText.Text.Trim();
                        prod.Price = price;
                        prod.TargetScreen = screenType;
                        prod.IsStockTracked = IsStockTrackedCheck.IsChecked ?? false;
                    }
                }
                db.SaveChanges();
            }
            LoadDataFromDatabase();
            CancelProduct_Click(null, null); // Formu Temizle
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ProductDisplayModel prod)
            {
                _editingProductId = prod.Id;
                ProductNameText.Text = prod.Name;
                ProductPriceText.Text = prod.Price.ToString();
                CategoryCombo.SelectedValue = prod.CategoryId;
                ProductTypeCombo.SelectedValue = prod.ProductTypeId;
                IsStockTrackedCheck.IsChecked = prod.IsStockTracked;

                foreach (ComboBoxItem item in TargetScreenCombo.Items)
                {
                    if (item.Tag.ToString() == prod.TargetScreen)
                    {
                        TargetScreenCombo.SelectedItem = item;
                        break;
                    }
                }

                BtnSaveProduct.Content = "GÜNCELLE";
                BtnSaveProduct.Background = new SolidColorBrush(Colors.Orange);
                BtnCancelProduct.Visibility = Visibility.Visible;
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is ProductDisplayModel prod)
            {
                using (var db = new AppDbContext())
                {
                    db.Products.Remove(db.Products.Find(prod.Id));
                    db.SaveChanges();
                }
                LoadDataFromDatabase();
            }
        }

        private void CancelProduct_Click(object sender, RoutedEventArgs e)
        {
            _editingProductId = null;
            ProductNameText.Clear();
            ProductPriceText.Clear();
            CategoryCombo.SelectedIndex = -1;
            ProductTypeCombo.SelectedIndex = -1;
            TargetScreenCombo.SelectedIndex = 0;
            IsStockTrackedCheck.IsChecked = false;

            BtnSaveProduct.Content = "ÜRÜN EKLE";
            BtnSaveProduct.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D7"));
            BtnCancelProduct.Visibility = Visibility.Collapsed;
        }

        private string GetTargetScreenDisplayName(TargetScreenType type)
        {
            return type switch
            {
                TargetScreenType.Kitchen => "Mutfak",
                TargetScreenType.Barista => "Barista",
                TargetScreenType.None => "Yok",
                _ => "Belirsiz"
            };
        }
    }

    public class ProductDisplayModel
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string TargetScreen { get; set; }
        public string TargetScreenText { get; set; }
        public bool IsStockTracked { get; set; }
        public string StockStatusText => IsStockTracked ? "Evet" : "Hayır";
    }
}