using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
 
namespace BuyOldBike_DAL.Repositories.Payment
{
    public class WalletTopUpRepository
    {
        private readonly BuyOldBikeContext _db = new();
 
        public Entities.Payment CreateTopUpPayment(Guid userId, decimal amount)
        {
            if (userId == Guid.Empty) throw new InvalidOperationException("Thiếu người dùng.");
            if (amount <= 0) throw new InvalidOperationException("Số tiền nạp phải lớn hơn 0.");
 
            var tx = _db.Database.BeginTransaction();
            try
            {
                var paymentId = Guid.NewGuid();
                var payment = new Entities.Payment
                {
                    PaymentId = paymentId,
                    UserId = userId,
                    OrderId = null,
                    Amount = amount,
                    PaymentType = StatusConstants.PaymentType.VN_Pay,
                    Status = StatusConstants.PaymentStatus.Pending,
                    TxnRef = paymentId.ToString("N"),
                    CreatedAt = DateTime.Now
                };
 
                _db.Payments.Add(payment);
                _db.SaveChanges();
                tx.Commit();
                return payment;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
 
        public void MarkTopUpSuccess(string txnRef, string? providerTxnNo)
        {
            if (string.IsNullOrWhiteSpace(txnRef)) throw new InvalidOperationException("Thiếu TxnRef.");
 
            var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var payment = _db.Payments
                    .FirstOrDefault(p =>
                        p.TxnRef == txnRef &&
                        p.PaymentType == StatusConstants.PaymentType.VN_Pay &&
                        p.OrderId == null
                    );
 
                if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch nạp ví theo TxnRef.");
                if (payment.Status == StatusConstants.PaymentStatus.Success)
                {
                    tx.Commit();
                    return;
                }
 
                if (payment.UserId == null || payment.UserId == Guid.Empty) throw new InvalidOperationException("Giao dịch thiếu người dùng.");
                var amount = payment.Amount ?? 0m;
                if (amount <= 0) throw new InvalidOperationException("Số tiền giao dịch không hợp lệ.");
 
                var wallet = _db.UserWallets.FirstOrDefault(w => w.UserId == payment.UserId.Value);
                if (wallet == null)
                {
                    wallet = new UserWallet
                    {
                        WalletId = Guid.NewGuid(),
                        UserId = payment.UserId.Value,
                        Balance = 0m,
                        UpdatedAt = DateTime.Now
                    };
                    _db.UserWallets.Add(wallet);
                    _db.SaveChanges();
                }
 
                wallet.Balance += amount;
                wallet.UpdatedAt = DateTime.Now;
 
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    Amount = amount,
                    Direction = "Credit",
                    Type = "TopUp",
                    Note = $"VNPay TopUp {txnRef}",
                    CreatedAt = DateTime.Now
                });
 
                payment.Status = StatusConstants.PaymentStatus.Success;
                payment.ProviderTxnNo = providerTxnNo;
 
                _db.SaveChanges();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
 
        public void MarkTopUpFailed(string txnRef)
        {
            if (string.IsNullOrWhiteSpace(txnRef)) throw new InvalidOperationException("Thiếu TxnRef.");
 
            var tx = _db.Database.BeginTransaction();
            try
            {
                var payment = _db.Payments
                    .FirstOrDefault(p =>
                        p.TxnRef == txnRef &&
                        p.PaymentType == StatusConstants.PaymentType.VN_Pay &&
                        p.OrderId == null
                    );
 
                if (payment == null) throw new InvalidOperationException("Không tìm thấy giao dịch nạp ví theo TxnRef.");
                if (payment.Status == StatusConstants.PaymentStatus.Success)
                {
                    tx.Commit();
                    return;
                }
 
                payment.Status = StatusConstants.PaymentStatus.Failed;
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
