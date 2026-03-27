using BuyOldBike_BLL.Services.Dispute;
using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BuyOldBike_Presentation.State;

namespace BuyOldBike_Presentation.Views
{
    public partial class InspectorWindow : Window
    {
        private readonly InspectionService _inspectionService = new InspectionService();
        private readonly DisputeService _disputeService = new DisputeService();
        private bool _isSyncingSelection;

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
                dgInspectionQueue.ItemsSource = pendingInspections;

                var disputes = _disputeService.GetAllPendingDisputes();
                dgDisputeList.ItemsSource = disputes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        private void dgPendingInspections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedInspection = dgPendingInspections.SelectedItem as Inspection;
            if (selectedInspection == null) return;

            SelectInspection(selectedInspection, true);
        }

        private void dgInspectionQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedInspection = dgInspectionQueue.SelectedItem as Inspection;
            if (selectedInspection == null) return;

            SelectInspection(selectedInspection, false);
        }

        private void SelectInspection(Inspection inspection, bool switchToInspectionTab)
        {
            if (_isSyncingSelection) return;

            try
            {
                _isSyncingSelection = true;

                if (!ReferenceEquals(dgPendingInspections.SelectedItem, inspection))
                {
                    dgPendingInspections.SelectedItem = inspection;
                }

                if (!ReferenceEquals(dgInspectionQueue.SelectedItem, inspection))
                {
                    dgInspectionQueue.SelectedItem = inspection;
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }

            LoadListingDetails(inspection.ListingId);

            if (switchToInspectionTab)
            {
                tabInspector.SelectedItem = tabInspection;
            }
        }

        private void LoadListingDetails(Guid listingId)
        {
            try
            {
                BuyOldBikeContext db = new BuyOldBikeContext();
                Listing? listing = db.Listings
                    .Include(l => l.Seller)
                    .Include(l => l.Brand)
                    .Include(l => l.BikeType)
                    .Include(l => l.ListingImages)
                    .AsNoTracking()
                    .FirstOrDefault(l => l.ListingId == listingId);

                pnlInspectionDetails.DataContext = listing;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết listing: {ex.Message}");
            }
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedInspection = dgInspectionQueue.SelectedItem as Inspection
                    ?? dgPendingInspections.SelectedItem as Inspection;
                if (selectedInspection == null)
                {
                    MessageBox.Show("Vui lòng chọn một đơn kiểm định từ danh sách!");
                    return;
                }

                int passCount = 0;
                if (rbFramePass.IsChecked == true) passCount++;
                if (rbBrakePass.IsChecked == true) passCount++;
                if (rbDrivetrainPass.IsChecked == true) passCount++;
                if (rbWheelAlignmentPass.IsChecked == true) passCount++;
                if (rbHandlebarSteeringPass.IsChecked == true) passCount++;

                bool isPassed = passCount >= 4;
                string? notes = txtNotes.Text;

                _inspectionService.ProcessInspection(selectedInspection.InspectionId, isPassed, passCount, notes);

                MessageBox.Show("Đã cập nhật kết quả kiểm định thành công!");
                
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý kiểm định: {ex.Message}");
            }
        }

        private void dgDisputeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDisputeList.SelectedItem is ReturnRequest request)
            {
                txtDisputeDetail.Text = request.Detail;
            }
            else
            {
                txtDisputeDetail.Text = "Chọn một đơn để xem chi tiết";
            }
        }

        private void BtnResolveDispute_Click(object sender, RoutedEventArgs e)
        {
            if (dgDisputeList.SelectedItem is not ReturnRequest request)
            {
                MessageBox.Show("Vui lòng chọn một khiếu nại để xử lý.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal percentage = (decimal)sldRefundPercentage.Value;

            try
            {
                _disputeService.ResolveDispute(request.ReturnRequestId, percentage);
                MessageBox.Show("Đã xử lý khiếu nại thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xử lý khiếu nại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutManager.Logout(this);
        }
    }
}
