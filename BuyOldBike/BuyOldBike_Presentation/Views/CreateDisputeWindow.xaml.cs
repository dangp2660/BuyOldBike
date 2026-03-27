using BuyOldBike_BLL.Services.Dispute;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BuyOldBike_Presentation.Services;
using Microsoft.Win32;

namespace BuyOldBike_Presentation.Views
{
    public partial class CreateDisputeWindow : Window
    {
        private readonly Guid _orderId;
        private readonly ObservableCollection<string> _selectedImagePaths = new ObservableCollection<string>();
        private readonly LocalMediaService _mediaService = new LocalMediaService("Uploads");
        private const int MaxImages = 5;

        public CreateDisputeWindow(Guid orderId)
        {
            InitializeComponent();
            _orderId = orderId;
            lstImages.ItemsSource = _selectedImagePaths;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnPickImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp",
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog(this) != true) return;

            var candidates = dialog.FileNames
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var path in candidates)
            {
                if (_selectedImagePaths.Count >= MaxImages) break;
                if (_selectedImagePaths.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase))) continue;
                _selectedImagePaths.Add(path);
            }

            if (_selectedImagePaths.Count >= MaxImages && candidates.Count > 0)
            {
                MessageBox.Show($"Tối đa {MaxImages} ảnh. Danh sách đã được giới hạn.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (lstImages.SelectedItem is not string selected) return;
            _selectedImagePaths.Remove(selected);
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            var reason = (cmbReason.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var detail = txtDetail.Text;

            if (string.IsNullOrWhiteSpace(detail))
            {
                MessageBox.Show("Vui lòng nhập chi tiết khiếu nại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedImagePaths.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 ảnh bằng chứng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var imageUrls = _selectedImagePaths.Select(p => _mediaService.SaveImage(p)).ToList();
                var disputeService = new DisputeService();
                disputeService.CreateDispute(_orderId, reason ?? "Khác", detail, imageUrls);

                MessageBox.Show("Đã gửi yêu cầu khiếu nại thành công! Vui lòng chờ phản hồi.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi gửi khiếu nại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
