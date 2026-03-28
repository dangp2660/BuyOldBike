using BuyOldBike_BLL.Services.Dispute;
using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.Services;
using Microsoft.Win32;
using System.Runtime.CompilerServices;

namespace BuyOldBike_Presentation.Views
{
    public partial class InspectorWindow : Window, INotifyPropertyChanged
    {
        private readonly InspectionService _inspectionService = new InspectionService();
        private readonly DisputeService _disputeService = new DisputeService();
        private readonly LocalMediaService _mediaService = new LocalMediaService("Uploads");
        private bool _isSyncingInspectionSelection;
        private bool _isSyncingDisputeSelection;
        private readonly List<string> _selectedInspectorImagePaths = [];
        private readonly List<string> _selectedInspectionReportImagePaths = [];
        private static readonly string[] DefaultComponentNames =
        [
            "Frame System",
            "Brake System",
            "Drivetrain",
            "Handlebar and Steering"
        ];

        private int _pendingInspectionCount;
        public int PendingInspectionCount
        {
            get => _pendingInspectionCount;
            private set
            {
                if (_pendingInspectionCount == value) return;
                _pendingInspectionCount = value;
                OnPropertyChanged();
            }
        }

        private int _completedInspectionCountLast30Days;
        public int CompletedInspectionCountLast30Days
        {
            get => _completedInspectionCountLast30Days;
            private set
            {
                if (_completedInspectionCountLast30Days == value) return;
                _completedInspectionCountLast30Days = value;
                OnPropertyChanged();
            }
        }

        private int _pendingDisputeCount;
        public int PendingDisputeCount
        {
            get => _pendingDisputeCount;
            private set
            {
                if (_pendingDisputeCount == value) return;
                _pendingDisputeCount = value;
                OnPropertyChanged();
            }
        }

        private List<ReturnRequest> _recentDisputes = [];
        public List<ReturnRequest> RecentDisputes
        {
            get => _recentDisputes;
            private set
            {
                if (ReferenceEquals(_recentDisputes, value)) return;
                _recentDisputes = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public InspectorWindow()
        {
            InitializeComponent();
            if (!RoleNavigator.EnsureRole(this, RoleConstants.Inspector)) return;
            DataContext = this;
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

                PendingInspectionCount = pendingInspections.Count;
                PendingDisputeCount = disputes.Count;
                RecentDisputes = disputes.Take(10).ToList();

                var fromDate = DateTime.Now.AddDays(-30);
                CompletedInspectionCountLast30Days = _inspectionService.CountCompletedInspectionsSince(fromDate);
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
            if (_isSyncingInspectionSelection) return;

            try
            {
                _isSyncingInspectionSelection = true;

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
                _isSyncingInspectionSelection = false;
            }

            LoadListingDetails(inspection.ListingId);
            ReloadInspectionReportImages(inspection.InspectionId);
            ClearInspectionReportSelection();

            if (switchToInspectionTab)
            {
                tabInspector.SelectedItem = tabInspection;
            }
        }

        private void dgRecentDisputes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRecentDisputes.SelectedItem is not ReturnRequest request) return;
            SelectDispute(request, true);
        }

        private void SelectDispute(ReturnRequest request, bool switchToDisputeTab)
        {
            if (_isSyncingDisputeSelection) return;

            try
            {
                _isSyncingDisputeSelection = true;

                var disputeItem = (dgDisputeList.ItemsSource as IEnumerable<ReturnRequest>)
                    ?.FirstOrDefault(r => r.ReturnRequestId == request.ReturnRequestId)
                    ?? request;

                if (!ReferenceEquals(dgDisputeList.SelectedItem, disputeItem))
                {
                    dgDisputeList.SelectedItem = disputeItem;
                }

                var recentItem = RecentDisputes.FirstOrDefault(r => r.ReturnRequestId == request.ReturnRequestId);
                if (recentItem != null && !ReferenceEquals(dgRecentDisputes.SelectedItem, recentItem))
                {
                    dgRecentDisputes.SelectedItem = recentItem;
                }
            }
            finally
            {
                _isSyncingDisputeSelection = false;
            }

            if (switchToDisputeTab)
            {
                tabInspector.SelectedItem = tabDisputeSupport;
            }
        }

        private void LoadListingDetails(Guid listingId)
        {
            try
            {
                Listing? listing = _inspectionService.GetListingDetailsForInspection(listingId);

                pnlInspectionDetails.DataContext = listing;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết listing: {ex.Message}");
            }
        }

        private void ReloadInspectionReportImages(Guid inspectionId)
        {
            icInspectionReportImages.ItemsSource = _inspectionService.GetInspectionImages(inspectionId);
        }

        private void ClearInspectionReportSelection()
        {
            _selectedInspectionReportImagePaths.Clear();
            icInspectionSelectedReportPreviews.ItemsSource = new List<string>();
        }

        private void ResetInspectionForm()
        {
            txtNotes.Text = string.Empty;

            rbFramePass.IsChecked = true;
            rbBrakePass.IsChecked = true;
            rbDrivetrainPass.IsChecked = true;
            rbHandlebarSteeringPass.IsChecked = true;

            ClearInspectionReportSelection();
            icInspectionReportImages.ItemsSource = new List<InspectionImage>();

            pnlInspectionDetails.DataContext = null;
            dgPendingInspections.SelectedItem = null;
            dgInspectionQueue.SelectedItem = null;
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
                if (rbHandlebarSteeringPass.IsChecked == true) passCount++;

                var componentResults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Frame System"] = rbFramePass.IsChecked == true,
                    ["Brake System"] = rbBrakePass.IsChecked == true,
                    ["Drivetrain"] = rbDrivetrainPass.IsChecked == true,
                    ["Handlebar and Steering"] = rbHandlebarSteeringPass.IsChecked == true
                };

                bool isPassed = passCount >= 3;
                string? notes = txtNotes.Text;

                var reportUrls = _selectedInspectionReportImagePaths
                    .Select(_mediaService.SaveImage)
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .ToList();

                _inspectionService.ProcessInspection(selectedInspection.InspectionId, isPassed, passCount, notes, componentResults, reportUrls);

                ReloadInspectionReportImages(selectedInspection.InspectionId);
                ClearInspectionReportSelection();

                MessageBox.Show("Đã cập nhật kết quả kiểm định thành công!");
                
                LoadData();
                ResetInspectionForm();
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
                var recentItem = RecentDisputes.FirstOrDefault(r => r.ReturnRequestId == request.ReturnRequestId);
                if (!_isSyncingDisputeSelection && recentItem != null)
                {
                    try
                    {
                        _isSyncingDisputeSelection = true;
                        dgRecentDisputes.SelectedItem = recentItem;
                    }
                    finally
                    {
                        _isSyncingDisputeSelection = false;
                    }
                }

                txtDisputeDetail.Text = request.Detail;
                ReloadDisputeImages(request.ReturnRequestId);
                LoadDisputeDetails(request.ReturnRequestId);
            }
            else
            {
                txtDisputeDetail.Text = "Chọn một đơn để xem chi tiết";
                icBuyerDisputeImages.ItemsSource = new List<ReturnRequestImage>();
                icInspectorDisputeImages.ItemsSource = new List<ReturnRequestImage>();
                icInspectorSelectedPreviews.ItemsSource = new List<string>();
                _selectedInspectorImagePaths.Clear();
                pnlDisputeDetails.DataContext = null;
            }
        }

        private void ReloadDisputeImages(Guid returnRequestId)
        {
            var result = _disputeService.GetDisputeImagesForInspector(returnRequestId);
            icBuyerDisputeImages.ItemsSource = result.BuyerImages;
            icInspectorDisputeImages.ItemsSource = result.InspectorImages;
        }

        private void LoadDisputeDetails(Guid returnRequestId)
        {
            try
            {
                var request = _disputeService.GetDisputeDetailsForInspector(returnRequestId);

                if (request?.Order?.Listing == null)
                {
                    pnlDisputeDetails.DataContext = new DisputeDetailVm();
                    return;
                }

                var listing = request.Order.Listing;
                var inspection = listing.Inspections
                    .OrderByDescending(i => i.CreatedAt)
                    .FirstOrDefault(i => i.Status == StatusConstants.InspectionStatus.Completed)
                    ?? listing.Inspections.OrderByDescending(i => i.CreatedAt).FirstOrDefault();

                var componentResults = new List<DisputeInspectionComponentVm>();
                foreach (var componentName in DefaultComponentNames)
                {
                    var score = inspection?.InspectionScores?
                        .FirstOrDefault(s => string.Equals(s.Component?.ComponentName, componentName, StringComparison.OrdinalIgnoreCase));

                    componentResults.Add(new DisputeInspectionComponentVm
                    {
                        ComponentName = componentName,
                        ResultText = score?.Score == 1 ? "Pass" : score?.Score == 0 ? "Fail" : "-"
                    });
                }

                var inspectionNote = inspection?.RejectReason;

                pnlDisputeDetails.DataContext = new DisputeDetailVm
                {
                    ListingTitle = listing.Title ?? "-",
                    OrderId = request.Order.OrderId,
                    SellerName = GetDisplayName(listing.Seller),
                    BuyerName = GetDisplayName(request.Order.Buyer),
                    InspectionResultText = inspection?.Result == StatusConstants.InspectionResult.Passed
                        ? "Pass"
                        : inspection?.Result == StatusConstants.InspectionResult.Failed
                            ? "Fail"
                            : "-",
                    InspectionOverallText = inspection?.OverallScore != null ? $"{inspection.OverallScore}/4" : "-",
                    InspectionNoteText = !string.IsNullOrWhiteSpace(inspectionNote) ? inspectionNote.Trim() : "-",
                    ComponentResults = componentResults
                };
            }
            catch
            {
                pnlDisputeDetails.DataContext = new DisputeDetailVm();
            }
        }

        private static string GetDisplayName(User? user)
        {
            var name = user?.Address?.FullName;
            if (!string.IsNullOrWhiteSpace(name)) return name.Trim();

            if (!string.IsNullOrWhiteSpace(user?.Email)) return user.Email.Trim();
            if (!string.IsNullOrWhiteSpace(user?.PhoneNumber)) return user.PhoneNumber.Trim();
            return "-";
        }

        private sealed class DisputeInspectionComponentVm
        {
            public string ComponentName { get; set; } = "-";
            public string ResultText { get; set; } = "-";
        }

        private sealed class DisputeDetailVm
        {
            public string ListingTitle { get; set; } = "-";
            public Guid OrderId { get; set; }
            public string SellerName { get; set; } = "-";
            public string BuyerName { get; set; } = "-";
            public string InspectionResultText { get; set; } = "-";
            public string InspectionOverallText { get; set; } = "-";
            public string InspectionNoteText { get; set; } = "-";
            public List<DisputeInspectionComponentVm> ComponentResults { get; set; } = [];
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

        private void BtnSelectInspectionReportImages_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };

            if (dlg.ShowDialog() != true) return;

            foreach (var f in dlg.FileNames)
            {
                if (_selectedInspectionReportImagePaths.Count >= 10) break;
                if (_selectedInspectionReportImagePaths.Contains(f, StringComparer.OrdinalIgnoreCase)) continue;
                _selectedInspectionReportImagePaths.Add(f);
            }

            icInspectionSelectedReportPreviews.ItemsSource = _selectedInspectionReportImagePaths.ToList();
        }

        private void BtnClearInspectionReportSelection_Click(object sender, RoutedEventArgs e)
        {
            ClearInspectionReportSelection();
        }

    }
}
