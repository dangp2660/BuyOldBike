using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Payment;
using System;

namespace BuyOldBike_BLL.Features.Payments.Wallet;

public class WalletService
{
    private readonly WalletRepository _repo = new WalletRepository();

    public decimal GetBalance(Guid userId)
    {
        return _repo.GetBalance(userId);
    }

    public decimal TopUp(Guid userId, decimal amount, string? note)
    {
        return _repo.TopUp(userId, amount, note);
    }

    public decimal Debit(Guid userId, decimal amount, string type, Guid? orderId, string? note)
    {
        return _repo.Debit(userId, amount, type, orderId, note);
    }

    public WalletTransaction[] GetRecentTransactions(Guid userId, int take)
    {
        return _repo.GetRecentTransactions(userId, take);
    }
}

