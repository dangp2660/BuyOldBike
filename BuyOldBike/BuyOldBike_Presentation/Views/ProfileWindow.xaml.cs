using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_BLL.Features.Payments.Wallet;
using BuyOldBike_BLL.Services.Kyc;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.Payments;
using BuyOldBike_Presentation.State;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
                var amount = row.DepositAmount ?? 0m;
                var confirm = MessageBox.Show(
                    $"Thanh toán đặt cọc {amount:N0}đ bằng VNPay hay ví?\n\nYes: VNPay\nNo: Ví\nCancel: Hủy",
                    "Xác nhận",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (confirm == MessageBoxResult.Cancel) return;

                if (confirm == MessageBoxResult.Yes)
                {
                    if (sender is FrameworkElement element) element.IsEnabled = false;

                    var options = VnPayOptionsLoader.LoadValidated();
                    var waitTask = VnPayReturnListener.WaitForReturnAsync(options.ReturnUrl, TimeSpan.FromMinutes(5));
                    var paymentUrl = depositService.BuildPaymentUrlForPendingDeposit(
                        AppSession.CurrentUser.UserId,
                        row.OrderId,
                        options,
                        "127.0.0.1"
                    );

                    Process.Start(new ProcessStartInfo { FileName = paymentUrl, UseShellExecute = true });

                    var query = await waitTask;
                    var ok = depositService.ProcessVnPayReturn(options, query, out var message);
                    MessageBox.Show(message, "VNPay", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    return;
                }

                depositService.PayPendingDepositWithWallet(AppSession.CurrentUser.UserId, row.OrderId);
                MessageBox.Show("Thanh toán thành công.", "Ví", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Hết thời gian chờ VNPay trả về.", "VNPay", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (sender is FrameworkElement element) element.IsEnabled = true;
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

        private sealed class WalletTransactionRow
        {
            public DateTime CreatedAt { get; init; }
            public string Type { get; init; } = "";
            public string Direction { get; init; } = "";
            public decimal Amount { get; init; }
            public string Note { get; init; } = "";

            public string DirectionText =>
                string.Equals(Direction, "Credit", StringComparison.OrdinalIgnoreCase) ? "Cộng" :
                string.Equals(Direction, "Debit", StringComparison.OrdinalIgnoreCase) ? "Trừ" : Direction;

            public string AmountText =>
                string.Equals(Direction, "Credit", StringComparison.OrdinalIgnoreCase) ? $"+{Amount:N0}đ" :
                string.Equals(Direction, "Debit", StringComparison.OrdinalIgnoreCase) ? $"-{Amount:N0}đ" : $"{Amount:N0}đ";
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
