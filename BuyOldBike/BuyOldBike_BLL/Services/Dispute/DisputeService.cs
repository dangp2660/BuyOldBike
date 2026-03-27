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
        public ReturnRequest CreateDispute(Guid orderId, string reason, string detail, IEnumerable<string> imageUrls)
        {
            var db = new BuyOldBikeContext();
            var order = db.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) throw new InvalidOperationException("Không tìm thấy đơn hàng.");
            
            if (order.Status != StatusConstants.OrdersStatus.Deposit_Paid)
            {
                throw new InvalidOperationException("Chỉ có thể khiếu nại đối với đơn hàng đã đặt cọc.");
            }

            var normalizedImageUrls = (imageUrls ?? Enumerable.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedImageUrls.Count == 0)
            {
                throw new InvalidOperationException("Khiếu nại cần có ít nhất 1 ảnh bằng chứng.");
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

            foreach (var url in normalizedImageUrls)
            {
                request.ReturnRequestImages.Add(new ReturnRequestImage
                {
                    ImageId = Guid.NewGuid(),
                    ReturnRequestId = request.ReturnRequestId,
                    ImageUrl = url,
                    UploaderRole = RoleConstants.Buyer,
                    CreatedAt = DateTime.Now
                });
            }

            db.ReturnRequests.Add(request);
            order.Status = StatusConstants.OrdersStatus.Disputed;

            db.SaveChanges();
            return request;
        }

        public void AddInspectorImages(Guid returnRequestId, IEnumerable<string> imageUrls)
        {
            using var db = new BuyOldBikeContext();
            var request = db.ReturnRequests
                .Include(r => r.ReturnRequestImages)
                .FirstOrDefault(r => r.ReturnRequestId == returnRequestId);

            if (request == null) throw new InvalidOperationException("Không tìm thấy yêu cầu khiếu nại.");
            if (request.Status != StatusConstants.ReturnRequestStatus.Pending)
            {
                throw new InvalidOperationException("Chỉ có thể thêm ảnh khi khiếu nại đang ở trạng thái Pending.");
            }

            var normalizedImageUrls = (imageUrls ?? Enumerable.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedImageUrls.Count == 0)
            {
                throw new InvalidOperationException("Vui lòng chọn ít nhất 1 ảnh.");
            }

            var existingUrls = request.ReturnRequestImages
                .Where(i => string.Equals(i.UploaderRole?.Trim(), RoleConstants.Inspector, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.ImageUrl)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var url in normalizedImageUrls)
            {
                if (existingUrls.Contains(url)) continue;

                request.ReturnRequestImages.Add(new ReturnRequestImage
                {
                    ImageId = Guid.NewGuid(),
                    ReturnRequestId = request.ReturnRequestId,
                    ImageUrl = url,
                    UploaderRole = RoleConstants.Inspector,
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();
        }

        public List<ReturnRequest> GetAllPendingDisputes()
        {
            using var db = new BuyOldBikeContext();
            return db.ReturnRequests
                .Include(r => r.Order!)
                .ThenInclude(o => o.Listing)
                .Include(r => r.ReturnRequestImages)
                .Where(r => r.Status == StatusConstants.ReturnRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public DisputeImageResult GetDisputeImagesForInspector(Guid returnRequestId)
        {
            using var db = new BuyOldBikeContext();

            var images = db.ReturnRequestImages
                .AsNoTracking()
                .Where(i => i.ReturnRequestId == returnRequestId)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            var buyerImages = images
                .Where(i => string.Equals(i.UploaderRole?.Trim(), RoleConstants.Buyer, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var inspectorDisputeUrls = images
                .Where(i => !string.Equals(i.UploaderRole?.Trim(), RoleConstants.Buyer, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.ImageUrl)
                .ToList();

            var request = db.ReturnRequests
                .Include(r => r.Order!)
                    .ThenInclude(o => o.Listing!)
                        .ThenInclude(l => l.Inspections)
                .AsNoTracking()
                .FirstOrDefault(r => r.ReturnRequestId == returnRequestId);

            var listing = request?.Order?.Listing;
            var inspection = listing?.Inspections?
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefault(i => i.Status == StatusConstants.InspectionStatus.Completed)
                ?? listing?.Inspections?.OrderByDescending(i => i.CreatedAt).FirstOrDefault();

            var inspectionUrls = inspection == null
                ? new List<string>()
                : db.InspectionImages
                    .AsNoTracking()
                    .Where(x => x.InspectionId == inspection.InspectionId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => x.ImageUrl)
                    .ToList();

            var combinedUrls = inspectionUrls
                .Concat(inspectorDisputeUrls)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(u => new ImageUrlItem { ImageUrl = u })
                .ToList();

            return new DisputeImageResult
            {
                BuyerImages = buyerImages,
                InspectorImages = combinedUrls
            };
        }

        public ReturnRequest? GetDisputeDetailsForInspector(Guid returnRequestId)
        {
            using var db = new BuyOldBikeContext();

            return db.ReturnRequests
                .Include(r => r.ReturnRequestImages)
                .Include(r => r.Order!)
                    .ThenInclude(o => o.Buyer!)
                        .ThenInclude(u => u.Address)
                .Include(r => r.Order!)
                    .ThenInclude(o => o.Listing!)
                        .ThenInclude(l => l.Seller!)
                            .ThenInclude(u => u.Address)
                .Include(r => r.Order!)
                    .ThenInclude(o => o.Listing!)
                        .ThenInclude(l => l.Inspections)
                            .ThenInclude(i => i.InspectionScores)
                                .ThenInclude(s => s.Component)
                .AsNoTracking()
                .FirstOrDefault(r => r.ReturnRequestId == returnRequestId);
        }

        public void ResolveDispute(Guid returnRequestId, decimal refundPercentage)
        {
            using var db = new BuyOldBikeContext();
            var transaction = db.Database.BeginTransaction();
            try
            {
                var request = db.ReturnRequests
                    .Include(r => r.Order!)
                    .ThenInclude(o => o.Payments)
                    .Include(r => r.Order!)
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
