using BuyOldBike_BLL.Services.Dispute;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BuyOldBike_Presentation.Views
{
    public partial class CreateDisputeWindow : Window
    {
        private readonly Guid _orderId;

        public CreateDisputeWindow(Guid orderId)
        {
            InitializeComponent();
            _orderId = orderId;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
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

            try
            {
                var disputeService = new DisputeService();
                disputeService.CreateDispute(_orderId, reason ?? "Khác", detail);

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