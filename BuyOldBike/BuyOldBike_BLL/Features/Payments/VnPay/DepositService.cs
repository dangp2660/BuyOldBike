using BuyOldBike_BLL.Features.Payments.VnPay;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Payment;
using BuyOldBike_DAL.Repositories.Seller;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BuyOldBike_BLL.Features.Payments
{
    public class DepositService
    {
        private readonly DepositRepository _depositRepo = new DepositRepository();
        private readonly BikePostRepository _listingRepo = new BikePostRepository();
        private readonly VnPayRequestBuilder _builder = new VnPayRequestBuilder();
        private readonly VnPayReturnVerifier _verifier = new VnPayReturnVerifier();
        private const decimal DepositRate = 0.2m;

        public (Guid orderId, string paymentUrl) CreateDepositPaymentUrl(Guid buyerId, Guid listingId, VnPayOptions options, string ipAddr)
        {
            var listing = _listingRepo.GetListingDetailById(listingId);
            if (listing?.Price == null || listing.Price <= 0) throw new InvalidOperationException("Giá listing không hợp lệ.");

            var depositAmount = Math.Round(listing.Price.Value * DepositRate, 0, MidpointRounding.AwayFromZero);
            var (order, payment) = _depositRepo.CreateDeposit(buyerId, listingId, depositAmount, null, null, null, null, null, null);

            var url = _builder.BuildPaymetUrl(options, new VnPayCreatePaymentRequest
            {
                AmountVnd = depositAmount,
                TxnRef = payment.TxnRef ?? order.OrderId.ToString("N"),
                OrderInfo = $"Dat coc {depositAmount.ToString("0", CultureInfo.InvariantCulture)} VND listing {listingId.ToString("N")}",
                IpAddr = ipAddr
            });

            return (order.OrderId, url);
        }

        public string BuildPaymentUrlForPendingDeposit(Guid buyerId, Guid orderId, VnPayOptions options, string ipAddr)
        {
            using var db = new BuyOldBikeContext();
            var order = db.Orders
                .AsNoTracking()
                .Include(o => o.Listing)
                .Include(o => o.Payments)
                .FirstOrDefault(o => o.OrderId == orderId && o.BuyerId == buyerId);

            if (order == null) throw new InvalidOperationException("Không tìm thấy đơn đặt cọc.");
            if (!string.Equals(order.Status, StatusConstants.OrdersStatus.Deposit_Pending, StringComparison.Ordinal))
                throw new InvalidOperationException("Đơn đặt cọc không ở trạng thái chờ thanh toán.");

            var payment = order.Payments?
                .Where(p => p.PaymentType == StatusConstants.PaymentType.VN_Pay)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch thanh toán VNPay.");
            if (!string.Equals(payment.Status, StatusConstants.PaymentStatus.Pending, StringComparison.Ordinal))
                throw new InvalidOperationException("Giao dịch không ở trạng thái chờ thanh toán.");

            var amount = payment.Amount ?? order.TotalAmount ?? 0m;
            if (amount <= 0) throw new InvalidOperationException("Số tiền không hợp lệ.");

            return _builder.BuildPaymetUrl(options, new VnPayCreatePaymentRequest
            {
                AmountVnd = amount,
                TxnRef = payment.TxnRef ?? order.OrderId.ToString("N"),
                OrderInfo = $"Dat coc {amount.ToString("0", CultureInfo.InvariantCulture)} VND listing {(order.ListingId.HasValue ? order.ListingId.Value.ToString("N") : "")}",
                IpAddr = ipAddr
            });
        }

        public bool ProcessVnPayReturn(VnPayOptions options, IReadOnlyDictionary<string, string> queryParameters, out string message)
        {
            var verify = _verifier.Verify(options, queryParameters);
            if (!verify.IsValidSignature) throw new InvalidOperationException(verify.Message ?? "Chữ ký không hợp lệ.");
            if (string.IsNullOrWhiteSpace(verify.TxnRef)) throw new InvalidOperationException("Thiếu vnp_TxnRef.");

            using var db = new BuyOldBikeContext();
            var payment = db.Payments
                .Include(p => p.Order)
                .FirstOrDefault(p =>
                    p.TxnRef == verify.TxnRef &&
                    p.PaymentType == StatusConstants.PaymentType.VN_Pay
                );

            if (payment == null)
            {
                throw new InvalidOperationException("Không tìm thấy giao dịch đặt cọc theo TxnRef.");
            }

            var order = payment.Order;
            if (order == null)
            {
                throw new InvalidOperationException("Không tìm thấy giao dịch đặt cọc theo TxnRef.");
            }

            if (verify.Amount.HasValue)
            {
                var expected = VnPayRequestBuilder.ToVnPayAmount(payment.Amount ?? 0m);
                if (verify.Amount.Value != expected) throw new InvalidOperationException("Số tiền VNPay trả về không khớp.");
            }

            if (verify.IsSuccess)
            {
                _depositRepo.MaskDepositSuccess(order.OrderId, verify.TransactionNo);
                message = verify.Message ?? "Thanh toán thành công.";
                return true;
            }

            _depositRepo.MaskDepositFailed(order.OrderId);
            message = verify.Message ?? "Thanh toán không thành công.";
            return false;
        }

        public void MarkDepositExpired(Guid orderId)
        {
            _depositRepo.MaskDepositExpried(orderId);
        }

        public void AutoRefundExpiredDeposits()
        {
            var expiredOrders = _depositRepo.GetExpiredDepositOrders();
            foreach (var orderId in expiredOrders)
            {
                try
                {
                    _depositRepo.RefundDepositDueToSellerNoShow(orderId);
                }
                catch
                {
                    // Ignore errors for individual refunds, continue with others
                }
            }
        }

        public void RefundDepositDueToSellerNoShow(Guid orderId)
        {
            _depositRepo.RefundDepositDueToSellerNoShow(orderId);
        }

        public void PayPendingDepositWithWallet(Guid buyerId, Guid orderId)
        {
            _depositRepo.PayDepositWithWallet(buyerId, orderId);
        }

        public void PlaceDepositWithWallet(Guid buyerId, Guid listingId, DeliveryAddressInfo deliveryAddress)
        {
            if (deliveryAddress == null) throw new InvalidOperationException("Thiếu địa chỉ nghiệm thu.");
            if (string.IsNullOrWhiteSpace(deliveryAddress.Province)) throw new InvalidOperationException("Vui lòng nhập tỉnh/thành phố.");
            if (string.IsNullOrWhiteSpace(deliveryAddress.District)) throw new InvalidOperationException("Vui lòng nhập quận/huyện.");
            if (string.IsNullOrWhiteSpace(deliveryAddress.Ward)) throw new InvalidOperationException("Vui lòng nhập phường/xã.");
            if (string.IsNullOrWhiteSpace(deliveryAddress.Detail)) throw new InvalidOperationException("Vui lòng nhập địa chỉ chi tiết.");

            var listing = _listingRepo.GetListingDetailById(listingId);
            if (listing?.Price == null || listing.Price <= 0) throw new InvalidOperationException("Giá listing không hợp lệ.");

            var depositAmount = Math.Round(listing.Price.Value * DepositRate, 0, MidpointRounding.AwayFromZero);
            
            // Check if wallet has enough money first
            using var db = new BuyOldBikeContext();
            var wallet = db.UserWallets.FirstOrDefault(w => w.UserId == buyerId);
            if (wallet == null || wallet.Balance < depositAmount)
                throw new InvalidOperationException("Số dư trong ví không đủ để đặt cọc. Vui lòng nạp thêm tiền.");

            // Create deposit
            var (order, payment) = _depositRepo.CreateDeposit(
                buyerId,
                listingId,
                depositAmount,
                deliveryAddress.FullName,
                deliveryAddress.PhoneNumber,
                deliveryAddress.Province,
                deliveryAddress.District,
                deliveryAddress.Ward,
                deliveryAddress.Detail
            );
            
            // Pay with wallet
            _depositRepo.PayDepositWithWallet(buyerId, order.OrderId);
        }
    }
}
