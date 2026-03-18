using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using System;
using System.Windows;
using BuyOldBike_Presentation.State;

namespace BuyOldBike_Presentation.Views
{
    public partial class InspectorWindow : Window
    {
        private readonly InspectionService _inspectionService = new InspectionService();

        public InspectorWindow()
        {
            InitializeComponent();
            if (!RoleNavigator.EnsureRole(this, RoleConstants.Inspector)) return;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                List<Inspection> pendingInspections = _inspectionService.GetPendingRequests();
                dgPendingInspections.ItemsSource = pendingInspections;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedInspection = dgPendingInspections.SelectedItem as Inspection;
                if (selectedInspection == null)
                {
                    MessageBox.Show("Vui lòng chọn một đơn kiểm định từ danh sách!");
                    return;
                }

                bool isPassed = rbPass.IsChecked == true;

                 _inspectionService.ProcessInspection(selectedInspection.InspectionId, isPassed);

                MessageBox.Show("Đã cập nhật kết quả kiểm định thành công!");
                
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý kiểm định: {ex.Message}");
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutManager.Logout(this);
        }
    }
}
