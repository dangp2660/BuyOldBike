using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_BLL.Services.Kyc;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.Payments;
using BuyOldBike_Presentation.State;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BuyOldBike_Presentation.Views
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var user = AppSession.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Bạn cần đăng nhập để xem hồ sơ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            txtEmail.Text = user.Email;
            txtPhone.Text = user.PhoneNumber;
            txtRole.Text = user.Role;

            var kycService = new KycProfileService();
            var profile = kycService.GetLatestProfile(user.UserId);
            if (profile == null)
            {
                txtStatus.Text = "Tài khoản này chưa có hồ sơ eKYC.";
                kycInfoSection.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtStatus.Text = "Đã tải hồ sơ eKYC.";
                FillKycInfo(profile);
            }

            LoadDepositOrders(user.UserId);
        }

        private void BtnRefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            var user = AppSession.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Bạn cần đăng nhập để xem đơn đặt cọc.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            LoadDepositOrders(user.UserId);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void BtnPayDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
            {
                MessageBox.Show("Bạn cần đăng nhập để thanh toán.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (sender is not FrameworkElement fe || fe.DataContext is not DepositOrderRow row)
            {
                return;
            }

            var depositService = new DepositService();
            try
            {
                var vnPay = VnPayOptionsLoader.LoadValidated();

                var paymentUrl = depositService.BuildPaymentUrlForPendingDeposit(
                    AppSession.CurrentUser.UserId,
                    row.OrderId,
                    vnPay,
                    "127.0.0.1"
                );

                try
                {
                    Clipboard.SetText(paymentUrl);
                }
                catch
                {
                }

                var waitTask = VnPayReturnListener.WaitForReturnAsync(vnPay.ReturnUrl, TimeSpan.FromMinutes(15));
                Process.Start(new ProcessStartInfo(paymentUrl) { UseShellExecute = true });
                var query = await waitTask;

                depositService.ProcessVnPayReturn(vnPay, query, out var message);
                MessageBox.Show(message);
            }
            catch (TimeoutException)
            {
                try
                {
                    depositService.MarkDepositExpired(row.OrderId);
                }
                catch
                {
                }

                MessageBox.Show("Hết thời gian chờ thanh toán. Vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                LoadDepositOrders(AppSession.CurrentUser.UserId);
            }
        }

        private void FillKycInfo(KycProfile profile)
        {
            txtIdNumber.Text = profile.IdNumber ?? "";
            txtFullName.Text = profile.FullName ?? "";
            txtDob.Text = profile.DateOfBirth ?? "";
            txtGender.Text = profile.Gender ?? "";
            txtNationality.Text = profile.Nationality ?? "";
            txtOrigin.Text = profile.PlaceOfOrigin ?? "";
            txtResidence.Text = profile.PlaceOfResidence ?? "";
            txtExpiry.Text = profile.ExpiryDate ?? "";
            txtVerifiedAt.Text = profile.VerifiedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "";
        }

        private void LoadDepositOrders(Guid userId)
        {
            try
            {
                txtDepositOrdersStatus.Text = "Đang tải đơn đặt cọc...";

                using var db = new BuyOldBikeContext();
                var orders = db.Orders
                    .AsNoTracking()
                    .Where(o => o.BuyerId == userId &&
                                (o.Status == StatusConstants.OrdersStatus.Deposit_Pending ||
                                 o.Status == StatusConstants.OrdersStatus.Deposit_Paid ||
                                 o.Status == StatusConstants.OrdersStatus.Deposit_Failed ||
                                 o.Status == StatusConstants.OrdersStatus.Deposit_Expired))
                    .Include(o => o.Listing)
                    .Include(o => o.Payments)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();

                var rows = orders.Select(o =>
                {
                    var payment = o.Payments?
                        .Where(p => p.PaymentType == StatusConstants.PaymentType.VN_Pay)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault();

                    return new DepositOrderRow
                    {
                        OrderId = o.OrderId,
                        TxnRef = payment?.TxnRef ?? o.OrderId.ToString("N"),
                        ListingTitle = o.Listing?.Title ?? "(Không có tiêu đề)",
                        DepositAmount = o.TotalAmount ?? payment?.Amount,
                        OrderStatus = o.Status ?? "",
                        PaymentStatus = payment?.Status ?? "",
                        CreatedAt = o.CreatedAt ?? payment?.CreatedAt,
                        ListingStatus = o.Listing?.Status ?? ""
                    };
                }).ToList();

                dgDepositOrders.ItemsSource = rows;

                if (rows.Count == 0)
                {
                    txtDepositOrdersStatus.Text = "Chưa có đơn đặt cọc nào.";
                }
                else
                {
                    txtDepositOrdersStatus.Text = $"Có {rows.Count} đơn đặt cọc.";
                }
            }
            catch (Exception ex)
            {
                txtDepositOrdersStatus.Text = $"Lỗi tải đơn đặt cọc: {ex.Message}";
                dgDepositOrders.ItemsSource = Array.Empty<DepositOrderRow>();
            }
        }

        private sealed class DepositOrderRow
        {
            public Guid OrderId { get; init; }
            public string TxnRef { get; init; } = "";
            public string ListingTitle { get; init; } = "";
            public decimal? DepositAmount { get; init; }
            public DateTime? CreatedAt { get; init; }
            public string OrderStatus { get; init; } = "";
            public string PaymentStatus { get; init; } = "";
            public string ListingStatus { get; init; } = "";

            public string OrderStatusText => MapOrderStatus(OrderStatus);
            public string PaymentStatusText => MapPaymentStatus(PaymentStatus);
            public string ListingStatusText => MapListingStatus(ListingStatus);
            public bool CanPay =>
                string.Equals(OrderStatus, StatusConstants.OrdersStatus.Deposit_Pending, StringComparison.Ordinal) &&
                string.Equals(PaymentStatus, StatusConstants.PaymentStatus.Pending, StringComparison.Ordinal);

            private static string MapOrderStatus(string? status)
            {
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Pending, StringComparison.Ordinal))
                    return "Đang chờ";
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Paid, StringComparison.Ordinal))
                    return "Đã đặt cọc";
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Failed, StringComparison.Ordinal))
                    return "Thất bại";
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Expired, StringComparison.Ordinal))
                    return "Hết hạn";
                return string.IsNullOrWhiteSpace(status) ? "--" : status;
            }

            private static string MapPaymentStatus(string? status)
            {
                if (string.Equals(status, StatusConstants.PaymentStatus.Pending, StringComparison.Ordinal))
                    return "Đang chờ";
                if (string.Equals(status, StatusConstants.PaymentStatus.Success, StringComparison.Ordinal))
                    return "Thành công";
                if (string.Equals(status, StatusConstants.PaymentStatus.Failed, StringComparison.Ordinal))
                    return "Thất bại";
                if (string.Equals(status, StatusConstants.PaymentStatus.Expired, StringComparison.Ordinal))
                    return "Hết hạn";
                if (string.Equals(status, StatusConstants.PaymentStatus.Completed, StringComparison.Ordinal))
                    return "Hoàn tất";
                return string.IsNullOrWhiteSpace(status) ? "--" : status;
            }

            private static string MapListingStatus(string? status)
            {
                if (string.Equals(status, StatusConstants.ListingStatus.Available, StringComparison.Ordinal))
                    return "Khả dụng";
                if (string.Equals(status, StatusConstants.ListingStatus.Deposit_Pending, StringComparison.Ordinal))
                    return "Chờ cọc";
                if (string.Equals(status, StatusConstants.ListingStatus.Reserved, StringComparison.Ordinal))
                    return "Đã giữ chỗ";
                if (string.Equals(status, StatusConstants.ListingStatus.Pending_Inspection, StringComparison.Ordinal))
                    return "Chờ kiểm định";
                if (string.Equals(status, StatusConstants.ListingStatus.Rejected, StringComparison.Ordinal))
                    return "Từ chối";
                return string.IsNullOrWhiteSpace(status) ? "--" : status;
            }
        }
    }
}
