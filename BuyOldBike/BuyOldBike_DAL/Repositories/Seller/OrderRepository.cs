using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Seller
{
    public class OrderRepository
    {
        private readonly BuyOldBikeContext _db;

        public OrderRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public List<Order> GetOrdersBySellerId(Guid sellerId)
        {
            return _db.Orders
                .Where(o => o.Listing != null && o.Listing.SellerId == sellerId)
                .Include(o => o.Buyer)
                .Include(o => o.Listing)
                    .ThenInclude(l => l.Seller)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }

        public void UpdateOrderStatus(Guid orderId, string newStatus)
        {
            var o = _db.Orders.Find(orderId);
            if (o == null) return;
            o.Status = newStatus;
            _db.SaveChanges();
        }

        public Order BuyBikeWithWallet(Guid buyerId, Guid listingId)
        {
            var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var alreadyBought = _db.Orders.Any(o =>
                    o.ListingId == listingId &&
                    o.BuyerId == buyerId &&
                    (o.Status == StatusConstants.OrdersStatus.Paid || o.Status == StatusConstants.OrdersStatus.Completed));
                if (alreadyBought) throw new InvalidOperationException("Đơn hàng đã được thanh toán trước đó.");

                var listing = _db.Listings.FirstOrDefault(l => l.ListingId == listingId);
                if (listing == null) throw new InvalidOperationException("Không tìm thấy listing.");
                if (listing.Status != StatusConstants.ListingStatus.Available && listing.Status != StatusConstants.ListingStatus.Reserved)
                    throw new InvalidOperationException("Listing không khả dụng để mua.");

                // If reserved, verify that the current buyer is the one who reserved it
                if (listing.Status == StatusConstants.ListingStatus.Reserved)
                {
                    var hasDeposit = _db.Orders.Any(o => o.ListingId == listingId && o.BuyerId == buyerId && o.Status == StatusConstants.OrdersStatus.Deposit_Paid);
                    if (!hasDeposit)
                        throw new InvalidOperationException("Listing đã được đặt cọc bởi người khác.");
                }

                if (listing.Price == null || listing.Price <= 0)
                    throw new InvalidOperationException("Giá listing không hợp lệ.");
                if (listing.SellerId == null) throw new InvalidOperationException("Listing thiếu thông tin người bán.");

                var totalPrice = listing.Price.Value;
                decimal depositAmount = 0m;

                // If user already paid deposit, subtract the deposit amount
                var depositOrder = _db.Orders.FirstOrDefault(o => o.ListingId == listingId && o.BuyerId == buyerId && o.Status == StatusConstants.OrdersStatus.Deposit_Paid);
                if (depositOrder != null && depositOrder.TotalAmount.HasValue)
                {
                    depositAmount = depositOrder.TotalAmount.Value;
                }

                var amount = totalPrice - depositAmount;
                if (amount < 0) amount = 0m;

                // Check wallet
                UserWallet? wallet = null;
                if (amount > 0)
                {
                    wallet = _db.UserWallets.FirstOrDefault(w => w.UserId == buyerId);
                    if (wallet == null || wallet.Balance < amount)
                        throw new InvalidOperationException("Số dư ví không đủ để mua xe.");
                }

                // Deduct balance
                if (amount > 0)
                {
                    wallet!.Balance -= amount;
                    wallet.UpdatedAt = DateTime.Now;
                }

                // Create Order
                Order order = new Order
                {
                    OrderId = Guid.NewGuid(),
                    BuyerId = buyerId,
                    ListingId = listingId,
                    Status = StatusConstants.OrdersStatus.Completed,
                    TotalAmount = amount,
                    CreatedAt = DateTime.Now,
                };
                _db.Orders.Add(order);

                if (amount > 0)
                {
                    // Create Payment
                    Entities.Payment payment = new Entities.Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        UserId = buyerId,
                        Amount = amount,
                        PaymentType = StatusConstants.PaymentType.Internal_Wallet,
                        Status = StatusConstants.PaymentStatus.Success,
                        CreatedAt = DateTime.Now
                    };

                    var walletTxnId = Guid.NewGuid();
                    payment.ProviderTxnNo = walletTxnId.ToString("N");
                    _db.Payments.Add(payment);

                    _db.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletTransactionId = walletTxnId,
                        WalletId = wallet!.WalletId,
                        Amount = amount,
                        Direction = "Debit",
                        Type = "BuyBike",
                        OrderId = order.OrderId,
                        Note = $"Mua xe {listingId.ToString("N")}",
                        CreatedAt = DateTime.Now
                    });
                }

                if (depositOrder != null)
                {
                    depositOrder.Status = StatusConstants.OrdersStatus.Deposit_Completed;
                }

                var sellerId = listing.SellerId.Value;
                var sellerWallet = _db.UserWallets.FirstOrDefault(w => w.UserId == sellerId);
                if (sellerWallet == null)
                {
                    sellerWallet = new UserWallet
                    {
                        WalletId = Guid.NewGuid(),
                        UserId = sellerId,
                        Balance = 0m,
                        UpdatedAt = DateTime.Now
                    };
                    _db.UserWallets.Add(sellerWallet);
                    _db.SaveChanges();
                }

                sellerWallet.Balance += totalPrice;
                sellerWallet.UpdatedAt = DateTime.Now;
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = sellerWallet.WalletId,
                    Amount = totalPrice,
                    Direction = "Credit",
                    Type = "SellBike",
                    OrderId = order.OrderId,
                    Note = $"Doanh thu bán xe {listingId.ToString("N")}",
                    CreatedAt = DateTime.Now
                });

                // Update listing status
                listing.Status = StatusConstants.ListingStatus.Sold;

                _db.SaveChanges();
                tx.Commit();
                return order;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
