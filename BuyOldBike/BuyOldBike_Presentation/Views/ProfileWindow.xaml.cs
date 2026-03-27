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
                                 o.Status == StatusConstants.OrdersStatus.Paid ||
                                 o.Status == StatusConstants.OrdersStatus.Deposit_Failed ||
                                 o.Status == StatusConstants.OrdersStatus.Deposit_Expired ||
                                 o.Status == StatusConstants.OrdersStatus.Disputed ||
                                 o.Status == StatusConstants.OrdersStatus.Dispute_Resolved))
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
                        ListingId = o.ListingId,
                        TxnRef = payment?.TxnRef ?? o.OrderId.ToString("N"),
                        ListingTitle = o.Listing?.Title ?? "(Không có tiêu đề)",
                        DepositAmount = o.TotalAmount ?? payment?.Amount,
                        OrderStatus = o.Status ?? "",
                        PaymentStatus = payment?.Status ?? "",
                        CreatedAt = o.CreatedAt ?? payment?.CreatedAt,
                        ListingStatus = o.Listing?.Status ?? "",
                        ListingPrice = o.Listing?.Price ?? 0m
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
            public Guid? ListingId { get; init; }
            public string TxnRef { get; init; } = "";
            public string ListingTitle { get; init; } = "";
            public decimal? DepositAmount { get; init; }
            public DateTime? CreatedAt { get; init; }
            public string OrderStatus { get; init; } = "";
            public string PaymentStatus { get; init; } = "";
            public string ListingStatus { get; init; } = "";
            public decimal ListingPrice { get; init; }

            public string OrderStatusText => MapOrderStatus(OrderStatus);
            public string PaymentStatusText => MapPaymentStatus(PaymentStatus);
            public string ListingStatusText => MapListingStatus(ListingStatus);
            
            public Visibility CanPayVisibility => 
                string.Equals(OrderStatus, StatusConstants.OrdersStatus.Deposit_Pending, StringComparison.Ordinal) &&
                string.Equals(PaymentStatus, StatusConstants.PaymentStatus.Pending, StringComparison.Ordinal)
                ? Visibility.Visible : Visibility.Collapsed;

            public Visibility CanBuyVisibility =>
                string.Equals(OrderStatus, StatusConstants.OrdersStatus.Deposit_Paid, StringComparison.Ordinal) &&
                string.Equals(ListingStatus, StatusConstants.ListingStatus.Reserved, StringComparison.Ordinal)
                ? Visibility.Visible : Visibility.Collapsed;

            public Visibility CanRefundVisibility =>
                string.Equals(OrderStatus, StatusConstants.OrdersStatus.Deposit_Paid, StringComparison.Ordinal) &&
                CreatedAt.HasValue && (DateTime.Now - CreatedAt.Value).TotalDays > 7
                ? Visibility.Visible : Visibility.Collapsed;

            private static string MapOrderStatus(string? status)
            {
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Pending, StringComparison.Ordinal))
                    return "Đang chờ";
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Paid, StringComparison.Ordinal))
                    return "Đã đặt cọc";
                if (string.Equals(status, StatusConstants.OrdersStatus.Paid, StringComparison.Ordinal))
                    return "Đã mua";
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Failed, StringComparison.Ordinal))
                    return "Thất bại";
                if (string.Equals(status, StatusConstants.OrdersStatus.Deposit_Expired, StringComparison.Ordinal))
                    return "Hết hạn";
                if (string.Equals(status, StatusConstants.OrdersStatus.Disputed, StringComparison.Ordinal))
                    return "Đang khiếu nại";
                if (string.Equals(status, StatusConstants.OrdersStatus.Dispute_Resolved, StringComparison.Ordinal))
                    return "Đã xử lý khiếu nại";
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
                if (string.Equals(status, StatusConstants.ListingStatus.Sold, StringComparison.Ordinal))
                    return "Đã bán";
                return string.IsNullOrWhiteSpace(status) ? "--" : status;
            }
        }

        private void BtnBuyBike_Click(object sender, RoutedEventArgs e)
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

            if (row.ListingId == null)
            {
                MessageBox.Show("Không tìm thấy thông tin xe.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn mua xe này bằng số dư trong ví? (Số tiền thanh toán sẽ được khấu trừ tiền đặt cọc)", "Xác nhận", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;

                var orderService = new BuyOldBike_BLL.Services.Seller.OrderService();
                orderService.BuyBikeWithWallet(AppSession.CurrentUser.UserId, row.ListingId.Value);

                MessageBox.Show("Mua xe thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDepositOrders(AppSession.CurrentUser.UserId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mua xe: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefundDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
            {
                MessageBox.Show("Bạn cần đăng nhập để thao tác.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (sender is not FrameworkElement fe || fe.DataContext is not DepositOrderRow row)
            {
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    "Đã quá hạn 7 ngày nhưng người bán vẫn chưa giao xe. Bạn có chắc chắn muốn yêu cầu hoàn cọc 100% về ví?",
                    "Xác nhận hoàn cọc",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var depositService = new DepositService();
                depositService.RefundDepositDueToSellerNoShow(row.OrderId);

                MessageBox.Show("Đã hoàn cọc 100% vào ví thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDepositOrders(AppSession.CurrentUser.UserId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hoàn cọc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDispute_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
            {
                MessageBox.Show("Bạn cần đăng nhập để thao tác.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (sender is not FrameworkElement fe || fe.DataContext is not DepositOrderRow row)
            {
                return;
            }

            var window = new CreateDisputeWindow(row.OrderId);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                LoadDepositOrders(AppSession.CurrentUser.UserId);
            }
        }
    }
}
