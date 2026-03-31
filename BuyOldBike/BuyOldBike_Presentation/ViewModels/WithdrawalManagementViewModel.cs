using BuyOldBike_BLL.Features.Payments.Wallet;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BuyOldBike_Presentation.ViewModels;

public class WithdrawalManagementViewModel : INotifyPropertyChanged
{
    private readonly WithdrawalRequestService _service;

    public ObservableCollection<WithdrawalRequestRow> Requests { get; } = new();

    public WithdrawalManagementViewModel(WithdrawalRequestService service)
    {
        _service = service;
    }

    public void LoadRequests()
    {
        try
        {
            var list = _service.GetAll();
            Requests.Clear();
            foreach (var r in list)
            {
                Requests.Add(new WithdrawalRequestRow(r));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi load danh sách rút tiền: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Confirm(Guid withdrawalRequestId)
    {
        try
        {
            _service.Confirm(withdrawalRequestId);
            MessageBox.Show("Đã xác nhận rút tiền.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadRequests();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public sealed class WithdrawalRequestRow
{
    public WithdrawalRequestRow(WithdrawalRequest entity)
    {
        WithdrawalRequestId = entity.WithdrawalRequestId;
        UserEmail = entity.User?.Email ?? "";
        Amount = entity.Amount;
        BankName = entity.BankName ?? "";
        AccountNumber = entity.AccountNumber ?? "";
        AccountName = entity.AccountName ?? "";
        Status = entity.Status ?? "";
        CreatedAt = entity.CreatedAt;
        ConfirmedAt = entity.ConfirmedAt;
    }

    public Guid WithdrawalRequestId { get; }
    public string UserEmail { get; }
    public decimal Amount { get; }
    public string BankName { get; }
    public string AccountNumber { get; }
    public string AccountName { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ConfirmedAt { get; }

    public string AmountText => $"{Amount:N0}đ";
}
