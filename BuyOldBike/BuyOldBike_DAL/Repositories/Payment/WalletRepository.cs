using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Payment;

public class WalletRepository
{
    private readonly BuyOldBikeContext _db = new();

    public UserWallet GetOrCreateWallet(Guid userId)
    {
        var wallet = _db.UserWallets.FirstOrDefault(w => w.UserId == userId);
        if (wallet != null) return wallet;

        wallet = new UserWallet
        {
            WalletId = Guid.NewGuid(),
            UserId = userId,
            Balance = 0m,
            UpdatedAt = DateTime.Now
        };
        _db.UserWallets.Add(wallet);
        _db.SaveChanges();
        return wallet;
    }

    public decimal GetBalance(Guid userId)
    {
        var wallet = GetOrCreateWallet(userId);
        return wallet.Balance;
    }

    public decimal TopUp(Guid userId, decimal amount, string? note)
    {
        if (amount <= 0) throw new InvalidOperationException("Số tiền nạp phải lớn hơn 0.");

        var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var wallet = _db.UserWallets.FirstOrDefault(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new UserWallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
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
                Note = note,
                CreatedAt = DateTime.Now
            });

            _db.SaveChanges();
            tx.Commit();
            return wallet.Balance;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public decimal Debit(Guid userId, decimal amount, string type, Guid? orderId, string? note)
    {
        if (amount <= 0) throw new InvalidOperationException("Số tiền trừ phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(type)) throw new InvalidOperationException("Thiếu loại giao dịch.");

        using var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var wallet = _db.UserWallets.FirstOrDefault(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new UserWallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0m,
                    UpdatedAt = DateTime.Now
                };
                _db.UserWallets.Add(wallet);
                _db.SaveChanges();
            }

            if (wallet.Balance < amount) throw new InvalidOperationException("Số dư không đủ.");

            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.Now;

            _db.WalletTransactions.Add(new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                Amount = amount,
                Direction = "Debit",
                Type = type,
                OrderId = orderId,
                Note = note,
                CreatedAt = DateTime.Now
            });

            _db.SaveChanges();
            tx.Commit();
            return wallet.Balance;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public WalletTransaction[] GetRecentTransactions(Guid userId, int take)
    {
        if (take <= 0) take = 20;

        var wallet = GetOrCreateWallet(userId);
        return _db.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletId == wallet.WalletId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .ToArray();
    }
}

