﻿using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.State;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BuyOldBike_Presentation.Views
{
    public partial class SellerWindow : Window
    {
        private readonly ListingService _listingService = new ListingService();
        private readonly List<string> _selectedImages = new List<string>();

        public SellerWindow()
        {
            InitializeComponent();
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

                _listingService.CreateNewPost(listing, savedPaths);

                MessageBox.Show("Đăng bài thành công! Bài đăng của bạn đang chờ kiểm duyệt.");
                
                txtTitle.Clear();
                txtDecription.Clear();
                txtPrice.Clear();
                txtUsage.Clear();
                _selectedImages.Clear();
                lblImageCount.Content = "Đã chọn 0 ảnh.";
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
                _selectedImages.AddRange(openFileDialog.FileNames);
                lblImageCount.Content = $"Đã chọn {_selectedImages.Count} ảnh.";
            }
        }

        public void Load()
        {
            // Load danh sách Brand (Giả định có BrandService hoặc dùng Context trực tiếp)
            using (var db = new BuyOldBike_DAL.Entities.BuyOldBikeContext())
            {
                var brands = db.Brands.ToList();
                cbxBrand.ItemsSource = brands;
                cbxBrand.SelectedValuePath = "BrandId";
                cbxBrand.DisplayMemberPath = "BrandName";
                if (brands.Any()) cbxBrand.SelectedIndex = 0;
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutManager.Logout(this);
        }
    }
}
