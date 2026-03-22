using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.Views
{
    public partial class ListingDetailWindow : Window
    {
        private readonly ListingDetailViewModel _vm = new ListingDetailViewModel();
        private readonly ListingService _listingService = new ListingService();
        private readonly Guid _listingId;
        private bool _isEditMode;

        public Action? OnSaved;

        public ListingDetailWindow(Guid listingId, bool editMode = false)
        {
            InitializeComponent();
            _listingId = listingId;
            _isEditMode = editMode;

            _vm.Load(listingId);
            if (_vm.ListingBike == null)
            {
                MessageBox.Show("Không tìm thấy listing.");
                Close();
                return;
            }

            DataContext = _vm;

            if (_isEditMode)
            {
                EnterEditMode();
            }
        }

        private void EnterEditMode()
        {
            // Show editable controls
            tbTitle.Visibility = Visibility.Collapsed;
            tbDesc.Visibility = Visibility.Collapsed;
            tbPrice.Visibility = Visibility.Collapsed;
            tbFrame.Visibility = Visibility.Collapsed;
            tbUsage.Visibility = Visibility.Collapsed;
            tbBrandType.Visibility = Visibility.Collapsed;

            txtTitle_Edit.Visibility = Visibility.Visible;
            txtDesc_Edit.Visibility = Visibility.Visible;
            txtPrice_Edit.Visibility = Visibility.Visible;
            txtFrame_Edit.Visibility = Visibility.Visible;
            txtUsage_Edit.Visibility = Visibility.Visible;
            cbxBrand_Edit.Visibility = Visibility.Visible;
            cbxType_Edit.Visibility = Visibility.Visible;
            btnSave.Visibility = Visibility.Visible;
            btnAddImage.Visibility = Visibility.Visible;

            // populate editable fields
            txtTitle_Edit.Text = _vm.ListingBike?.Title ?? string.Empty;
            txtDesc_Edit.Text = _vm.ListingBike?.Description ?? string.Empty;
            txtPrice_Edit.Text = (_vm.ListingBike?.Price ?? 0).ToString();
            txtFrame_Edit.Text = _vm.ListingBike?.FrameNumber ?? string.Empty;
            txtUsage_Edit.Text = _vm.ListingBike?.UsageDuration?.ToString() ?? string.Empty;

            using var db = new BuyOldBike_DAL.Entities.BuyOldBikeContext();

            // load brands into cbxBrand_Edit
            var brands = db.Brands.ToList();
            cbxBrand_Edit.ItemsSource = brands;
            cbxBrand_Edit.SelectedValuePath = "BrandId";
            cbxBrand_Edit.DisplayMemberPath = "BrandName";
            if (_vm.ListingBike?.BrandId != null)
            {
                cbxBrand_Edit.SelectedValue = _vm.ListingBike.BrandId;
            }

            // load bike types into cbxType_Edit
            var types = db.Types.ToList();
            cbxType_Edit.ItemsSource = types;
            cbxType_Edit.SelectedValuePath = "BikeTypeId";
            cbxType_Edit.DisplayMemberPath = "Name";
            if (_vm.ListingBike?.BikeTypeId != null)
            {
                cbxType_Edit.SelectedValue = _vm.ListingBike.BikeTypeId;
            }
        }

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            var paths = new List<string>();
            foreach (var f in dialog.FileNames)
            {
                try
                {
                    // copy to temp and add
                    string dest = Path.Combine(Path.GetTempPath(), $"postimg_{Guid.NewGuid()}{Path.GetExtension(f)}");
                    File.Copy(f, dest, true);
                    paths.Add(dest);

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(dest);
                    bmp.EndInit();
                    bmp.Freeze();
                    _vm.Images.Add(bmp);
                }
                catch { }
            }

            if (paths.Any())
            {
                _listingService.AddImagesToListing(_listingId, paths);
                MessageBox.Show($"Đã thêm {paths.Count} ảnh.");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.ListingBike == null) return;

                if (!decimal.TryParse(txtPrice_Edit.Text, out decimal price))
                {
                    MessageBox.Show("Giá không hợp lệ.");
                    return;
                }

                _vm.ListingBike.Title = txtTitle_Edit.Text.Trim();
                _vm.ListingBike.Description = txtDesc_Edit.Text.Trim();
                _vm.ListingBike.Price = price;
                _vm.ListingBike.FrameNumber = txtFrame_Edit.Text.Trim();
                if (int.TryParse(txtUsage_Edit.Text, out int months)) _vm.ListingBike.UsageDuration = months;
                if (cbxBrand_Edit.SelectedValue is int bid) _vm.ListingBike.BrandId = bid;
                if (cbxType_Edit.SelectedValue is int tid) _vm.ListingBike.BikeTypeId = tid;

                // set status back to pending inspection for re-approval
                _vm.ListingBike.Status = BuyOldBike_DAL.Constants.StatusConstants.ListingStatus.Pending_Inspection;

                // call listing service to update
                _listingService.UpdateListing(_vm.ListingBike);

                MessageBox.Show("Cập nhật bài đăng thành công.");

                OnSaved?.Invoke();

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu bài đăng: {ex.Message}");
            }
        }
    }
}
