using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Payment;
using System;

namespace BuyOldBike_BLL.Features.Payments.Wallet;

public class WithdrawalRequestService
{
    private readonly WithdrawalRepository _repo;

    public WithdrawalRequestService() : this(new WithdrawalRepository())
    {
    }

    public WithdrawalRequestService(WithdrawalRepository repo)
    {
        _repo = repo;
    }

    public WithdrawalRequest CreateRequest(Guid userId, decimal amount, string bankName, string accountNumber, string accountName)
    {
        return _repo.CreateRequest(userId, amount, bankName, accountNumber, accountName);
    }

    public WithdrawalRequest[] GetAll()
    {
        return _repo.GetAll();
    }

    public void Confirm(Guid withdrawalRequestId)
    {
        _repo.Confirm(withdrawalRequestId);
    }
}

