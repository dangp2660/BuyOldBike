using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuyOldBike_BLL.Services.Dispute
{
    public class DisputeService
    {
        public ReturnRequest CreateDispute(Guid orderId, string reason, string detail)
        {
            var db = new BuyOldBikeContext();
            var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) throw new InvalidOperationException("Không tìm thấy đơn hàng.");
            
            if (order.Status != StatusConstants.OrdersStatus.Deposit_Paid)
            {
                throw new InvalidOperationException("Chỉ có thể khiếu nại đối với đơn hàng đã đặt cọc.");
            }

            var request = new ReturnRequest
            {
                ReturnRequestId = Guid.NewGuid(),
                OrderId = orderId,
                Reason = reason,
                Detail = detail,
                Status = StatusConstants.ReturnRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            db.ReturnRequests.Add(request);
            order.Status = StatusConstants.OrdersStatus.Disputed;

            db.SaveChanges();
            return request;
        }

        public List<ReturnRequest> GetAllPendingDisputes()
        {
            using var db = new BuyOldBikeContext();
            return db.ReturnRequests
                .Include(r => r.Order)
                .ThenInclude(o => o.Listing)
                .Where(r => r.Status == StatusConstants.ReturnRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public void ResolveDispute(Guid returnRequestId, decimal refundPercentage)
        {
            using var db = new BuyOldBikeContext();
            var transaction = db.Database.BeginTransaction();
            try
            {
                var request = db.ReturnRequests
                    .Include(r => r.Order)
                    .ThenInclude(o => o.Payments)
                    .Include(r => r.Order)
                    .ThenInclude(o => o.Listing)
                    .FirstOrDefault(r => r.ReturnRequestId == returnRequestId);

                if (request == null) throw new InvalidOperationException("Không tìm thấy yêu cầu khiếu nại.");
                if (request.Status != StatusConstants.ReturnRequestStatus.Pending)
                    throw new InvalidOperationException("Yêu cầu này đã được xử lý.");

                var order = request.Order;
                if (order == null) throw new InvalidOperationException("Đơn hàng không hợp lệ.");

                if (refundPercentage > 0)
                {
                    var successPayment = order.Payments?
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault(p => p.Status == StatusConstants.PaymentStatus.Success);

                    if (successPayment == null) throw new InvalidOperationException("Không tìm thấy giao dịch đặt cọc thành công.");
                    var depositAmount = successPayment.Amount ?? order.TotalAmount ?? 0m;
                    var refundAmount = depositAmount * (refundPercentage / 100m);

                    if (order.BuyerId == null) throw new InvalidOperationException("Đơn hàng không có người mua.");
                    var buyerId = order.BuyerId.Value;

                    var wallet = db.UserWallets.FirstOrDefault(w => w.UserId == buyerId);
                    if (wallet == null)
                    {
                        wallet = new UserWallet
                        {
                            WalletId = Guid.NewGuid(),
                            UserId = buyerId,
                            Balance = 0m,
                            UpdatedAt = DateTime.Now
                        };
                        db.UserWallets.Add(wallet);
                    }

                    wallet.Balance += refundAmount;
                    wallet.UpdatedAt = DateTime.Now;

                    var refundTxnId = Guid.NewGuid();
                    db.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletTransactionId = refundTxnId,
                        WalletId = wallet.WalletId,
                        Amount = refundAmount,
                        Direction = "Credit",
                        Type = "Refund",
                        OrderId = order.OrderId,
                        Note = $"Hoàn cọc {refundPercentage}% do giải quyết khiếu nại đơn {order.OrderId.ToString("N")}",
                        CreatedAt = DateTime.Now
                    });

                    db.Payments.Add(new Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        UserId = buyerId,
                        Amount = refundAmount,
                        PaymentType = StatusConstants.PaymentType.Internal_Wallet,
                        Status = StatusConstants.PaymentStatus.Success,
                        ProviderTxnNo = refundTxnId.ToString("N"),
                        CreatedAt = DateTime.Now
                    });
                }

                request.Status = StatusConstants.ReturnRequestStatus.Resolved;
                order.Status = StatusConstants.OrdersStatus.Dispute_Resolved;

                if (order.Listing != null)
                {
                    order.Listing.Status = StatusConstants.ListingStatus.Available;
                }

                db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}