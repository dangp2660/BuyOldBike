﻿﻿using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.ViewModels;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.Views
{
    public partial class SellerWindow : Window
    {
        private readonly SellerWindowViewModel _vm = new SellerWindowViewModel();
        private readonly List<string> _selectedImages = new List<string>();

        public SellerWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            if (!RoleNavigator.EnsureRole(this, RoleConstants.Seller)) return;
            Load();
        }

        private void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AppSession.CurrentUser == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập trước khi đăng bài!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtPrice.Text))
                {
                    MessageBox.Show("Vui lòng nhập tiêu đề và giá sản phẩm!");
                    return;
                }

                List<string> savedPaths = new List<string>();
                string uploadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                foreach (string path in _selectedImages)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(path);
                    string destPath = Path.Combine(uploadDir, fileName);
                    File.Copy(path, destPath, true);
                    savedPaths.Add("Uploads/" + fileName);
                }

                Listing listing = new Listing
                {
                    Title = txtTitle.Text,
                    Description = txtDecription.Text, 
                    Price = decimal.TryParse(txtPrice.Text, out decimal price) ? price : 0,
                    BrandId = cbxBrand.SelectedValue != null ? (int)cbxBrand.SelectedValue : null,
                    FrameNumber = (cbxFrameSize.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                    SellerId = AppSession.CurrentUser.UserId,
                    UsageDuration = int.TryParse(txtUsage.Text, out int usage) ? usage : 0,
                };
                bool ok = MessageBox.Show("Bạn có chắc chắn đăng xe này không?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
                if(!ok) return;
                _vm.CreateNewPost(listing, savedPaths);

                MessageBox.Show("Đăng bài thành công! Bài đăng của bạn đang chờ kiểm duyệt.");
                var detailWindow = new ListingDetailWindow(listing.ListingId) { Owner = this };
                detailWindow.ShowDialog();
                
                txtTitle.Clear();
                txtDecription.Clear();
                txtPrice.Clear();
                txtUsage.Clear();
                _selectedImages.Clear();
                _vm.SelectedImagePreviews.Clear();
                lblImageCount.Content = "Đã chọn 0 ảnh.";
                LoadSellerListings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}");
            }
        }

        private void btnUpImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var path in openFileDialog.FileNames)
                {
                    if (_selectedImages.Contains(path)) continue;
                    _selectedImages.Add(path);
                    _vm.SelectedImagePreviews.Add(CreatePreviewImage(path));
                }
                lblImageCount.Content = $"Đã chọn {_selectedImages.Count} ảnh.";
            }
        }

        private static BitmapImage CreatePreviewImage(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        public void Load()
        {
            BuyOldBikeContext db = new BuyOldBikeContext();
            var brands = db.Brands.ToList();
            cbxBrand.ItemsSource = brands;
            cbxBrand.SelectedValuePath = "BrandId";
            cbxBrand.DisplayMemberPath = "BrandName";
            if (brands.Any()) cbxBrand.SelectedIndex = 0;
            LoadSellerListings();
        }

        private void LoadSellerListings()
        {
            if (AppSession.CurrentUser == null) return;
            _vm.LoadSellerListings(AppSession.CurrentUser.UserId);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutManager.Logout(this);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            txtTitle.Text = string.Empty;
            txtPrice.Text = string.Empty;
            txtDecription.Text = string.Empty;
            cbxBrand.SelectedIndex = 0;
            cbxFrameSize.SelectedIndex = 0;
            txtUsage.Text = string.Empty;
            _vm.SelectedImagePreviews.Clear();
        }

    }
}
