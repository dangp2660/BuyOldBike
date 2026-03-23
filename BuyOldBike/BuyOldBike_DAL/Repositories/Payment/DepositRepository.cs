using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;


namespace BuyOldBike_DAL.Repositories.Payment
{
    public class DepositRepository
    {
        private BuyOldBikeContext _db = new();
        public DepositRepository()
        {
           
        }
        public (Order, Entities.Payment) CreateDeposit(Guid buyerId, Guid listingId, decimal depositAmount)
        {
            var transaction = _db.Database.BeginTransaction();
            try
            {
                Listing listing = _db.Listings.FirstOrDefault(l => l.ListingId == listingId);
                if (listing == null) throw new InvalidOperationException("Không tìm thấy listing");
                if (!string.Equals(listing.Status, StatusConstants.ListingStatus.Available, StringComparison.Ordinal))
                    throw new InvalidOperationException("Listing không khả dụng để đặt cọc");
                listing.Status = StatusConstants.ListingStatus.Deposit_Pending;

                Order order = new Order 
                {
                    OrderId = Guid.NewGuid(),
                    BuyerId = buyerId,
                    ListingId = listingId,
                    Status = StatusConstants.OrdersStatus.Deposit_Pending,
                    TotalAmount = depositAmount,
                    CreatedAt = DateTime.Now,
                };

                Entities.Payment payment = new Entities.Payment
                {
                    PaymentId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    UserId = buyerId,
                    Amount = depositAmount,
                    PaymentType = StatusConstants.PaymentType.VN_Pay,
                    Status = StatusConstants.PaymentStatus.Pending,
                    TxnRef = order.OrderId.ToString("N"),
                    CreatedAt = DateTime.Now
                };
                _db.Orders.Add(order);
                _db.Payments.Add(payment);
                _db.SaveChanges();
                transaction.Commit();
                return (order, payment);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public void MaskDepositSuccess(Guid orderId, string? providerTxnNo)
        {
            var transaction = _db.Database.BeginTransaction();
            try
            {
                Order order = _db.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null) throw new InvalidOperationException("Không tìm thấy đơn hàng.");
                Entities.Payment payment = _db.Payments.FirstOrDefault(p => p.OrderId == orderId &&
                p.PaymentType == StatusConstants.PaymentType.VN_Pay);

                if(payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch thanh toán.");

                if(payment.Status == StatusConstants.PaymentStatus.Success)
                {
                    transaction.Commit();
                    return;
                }
                
                order.Status = StatusConstants.OrdersStatus.Deposit_Paid;
                payment.Status = StatusConstants.PaymentStatus.Success;
                payment.ProviderTxnNo = providerTxnNo;

                Listing listing = _db.Listings.FirstOrDefault(l => l.ListingId == order.ListingId);
                if (listing != null) listing.Status = StatusConstants.ListingStatus.Reserved;
                _db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public void MaskDepositToNewStatus(Guid orderId, string orderStatus, string paymentStatus)
        {
            var transaction = _db.Database.BeginTransaction();
            try
            {
                Order order = _db.Orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null) throw new InvalidOperationException("Không tìm thấy đơn hàng.");

                Entities.Payment payment = _db.Payments.FirstOrDefault(p => p.OrderId == orderId &&
                p.PaymentType == StatusConstants.PaymentType.VN_Pay);

                if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch thanh toán.");

                if (payment.Status == StatusConstants.PaymentStatus.Success)
                {
                    transaction.Commit();
                    return;
                }

                order.Status = orderStatus;
                payment.Status = paymentStatus;

                Listing listing = _db.Listings.FirstOrDefault(l => l.ListingId == order.ListingId
                && l.Status.Equals(StatusConstants.ListingStatus.Deposit_Pending));
                if(listing != null)
                {
                    listing.Status = StatusConstants.ListingStatus.Available;
                }
                _db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        public void MaskDepositFailed(Guid orderId)
        {
            MaskDepositToNewStatus(orderId,StatusConstants.OrdersStatus.Deposit_Failed,
                StatusConstants.PaymentStatus.Failed);
        }

        public void MaskDepositExpried(Guid orderId)
        {
            MaskDepositToNewStatus(orderId, StatusConstants.OrdersStatus.Deposit_Expired,
                StatusConstants.PaymentStatus.Expired);
        }

    }
}
