using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Payment;

public class WithdrawalRepository
{
    private readonly BuyOldBikeContext _db = new();

    public WithdrawalRequest CreateRequest(Guid userId, decimal amount, string bankName, string accountNumber, string accountName)
    {
        if (amount <= 0) throw new InvalidOperationException("Số tiền rút phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(bankName)) throw new InvalidOperationException("Vui lòng nhập tên ngân hàng.");
        if (string.IsNullOrWhiteSpace(accountNumber)) throw new InvalidOperationException("Vui lòng nhập số tài khoản.");
        if (string.IsNullOrWhiteSpace(accountName)) throw new InvalidOperationException("Vui lòng nhập tên chủ tài khoản.");

        var req = new WithdrawalRequest
        {
            WithdrawalRequestId = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            BankName = bankName.Trim(),
            AccountNumber = accountNumber.Trim(),
            AccountName = accountName.Trim(),
            Status = StatusConstants.WithdrawalRequestStatus.Pending,
            CreatedAt = DateTime.Now,
            ConfirmedAt = null
        };

        _db.WithdrawalRequests.Add(req);
        _db.SaveChanges();
        return req;
    }

    public WithdrawalRequest[] GetAll()
    {
        return _db.WithdrawalRequests
            .AsNoTracking()
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToArray();
    }

    public void Confirm(Guid withdrawalRequestId)
    {
        using var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var req = _db.WithdrawalRequests.FirstOrDefault(r => r.WithdrawalRequestId == withdrawalRequestId);
            if (req == null) throw new InvalidOperationException("Không tìm thấy yêu cầu rút tiền.");
            if (!string.Equals(req.Status, StatusConstants.WithdrawalRequestStatus.Pending, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Yêu cầu rút tiền không còn ở trạng thái Pending.");

            var wallet = _db.UserWallets.FirstOrDefault(w => w.UserId == req.UserId);
            if (wallet == null)
            {
                wallet = new UserWallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = req.UserId,
                    Balance = 0m,
                    UpdatedAt = DateTime.Now
                };
                _db.UserWallets.Add(wallet);
                _db.SaveChanges();
            }

            if (wallet.Balance < req.Amount) throw new InvalidOperationException("Số dư không đủ để rút tiền.");

            wallet.Balance -= req.Amount;
            wallet.UpdatedAt = DateTime.Now;

            var note = $"Rút tiền: {req.BankName} - {req.AccountNumber} - {req.AccountName}";
            _db.WalletTransactions.Add(new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                Amount = req.Amount,
                Direction = "Debit",
                Type = "Withdraw",
                OrderId = null,
                Note = note,
                CreatedAt = DateTime.Now
            });

            req.Status = StatusConstants.WithdrawalRequestStatus.Confirmed;
            req.ConfirmedAt = DateTime.Now;

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

