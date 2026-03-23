using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;


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
                Entities.Payment payment = _db.Payments.FirstOrDefault(p => p.OrderId == orderId);

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

                Entities.Payment payment = _db.Payments.FirstOrDefault(p => p.OrderId == orderId);

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


        public void PayDepositWithWallet(Guid buyerId, Guid orderId)
        {
            var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var order = _db.Orders
                    .Include(o => o.Listing)
                    .Include(o => o.Payments)
                    .FirstOrDefault(o => o.OrderId == orderId && o.BuyerId == buyerId);

                if (order == null) throw new InvalidOperationException("Không tìm thấy đơn đặt cọc.");
                if (!string.Equals(order.Status, StatusConstants.OrdersStatus.Deposit_Pending, StringComparison.Ordinal))
                    throw new InvalidOperationException("Đơn đặt cọc không ở trạng thái chờ thanh toán.");

                var payment = order.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();
                if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch thanh toán.");
                if (!string.Equals(payment.Status, StatusConstants.PaymentStatus.Pending, StringComparison.Ordinal))
                    throw new InvalidOperationException("Giao dịch không ở trạng thái chờ thanh toán.");

                var amount = payment.Amount ?? order.TotalAmount ?? 0m;
                if (amount <= 0) throw new InvalidOperationException("Số tiền không hợp lệ.");

                var wallet = _db.UserWallets.FirstOrDefault(w => w.UserId == buyerId);
                if (wallet == null)
                {
                    wallet = new UserWallet
                    {
                        WalletId = Guid.NewGuid(),
                        UserId = buyerId,
                        Balance = 0m,
                        UpdatedAt = DateTime.Now
                    };
                    _db.UserWallets.Add(wallet);
                    _db.SaveChanges();
                }

                if (wallet.Balance < amount) throw new InvalidOperationException("Số dư không đủ.");

                wallet.Balance -= amount;
                wallet.UpdatedAt = DateTime.Now;

                var walletTxnId = Guid.NewGuid();
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    WalletTransactionId = walletTxnId,
                    WalletId = wallet.WalletId,
                    Amount = amount,
                    Direction = "Debit",
                    Type = "Deposit",
                    OrderId = orderId,
                    Note = $"Thanh toán đặt cọc {orderId.ToString("N")}",
                    CreatedAt = DateTime.Now
                });

                order.Status = StatusConstants.OrdersStatus.Deposit_Paid;
                payment.PaymentType = StatusConstants.PaymentType.Internal_Wallet;
                payment.Status = StatusConstants.PaymentStatus.Success;
                payment.ProviderTxnNo = walletTxnId.ToString("N");

                if (order.Listing != null) order.Listing.Status = StatusConstants.ListingStatus.Reserved;

                _db.SaveChanges();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
