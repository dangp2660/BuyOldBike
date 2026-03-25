using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_BLL.Features.Payments.Wallet;
using BuyOldBike_Presentation.Payments;
using BuyOldBike_Presentation.State;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace BuyOldBike_Presentation.Views;

public partial class WalletWindow : Window
{
    private bool _isTopUpAmountFormatting;

    public WalletWindow()
    {
        InitializeComponent();
        DataObject.AddPastingHandler(txtTopUpAmount, TxtTopUpAmount_Pasting);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
        {
            MessageBox.Show("Bạn cần đăng nhập để dùng ví.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
            return;
        }

        LoadWallet(AppSession.CurrentUser.UserId);
    }

    private async void BtnTopUp_Click(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
        {
            MessageBox.Show("Bạn cần đăng nhập để nạp tiền.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var digits = new string((txtTopUpAmount.Text ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 0 ||
            !decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) ||
            amount <= 0)
        {
            MessageBox.Show("Số tiền nạp không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (sender is Button btn) btn.IsEnabled = false;

            var options = VnPayOptionsLoader.LoadValidated();
            var topUpService = new WalletTopUpVnPayService();

            var waitTask = VnPayReturnListener.WaitForReturnAsync(options.ReturnUrl, TimeSpan.FromMinutes(5));
            var paymentUrl = topUpService.CreateTopUpPaymentUrl(AppSession.CurrentUser.UserId, amount, options, "127.0.0.1");

            Process.Start(new ProcessStartInfo { FileName = paymentUrl, UseShellExecute = true });

            var query = await waitTask;
            var ok = topUpService.ProcessVnPayReturn(options, query, out var message);
            LoadWallet(AppSession.CurrentUser.UserId);

            MessageBox.Show(message, "VNPay", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (TimeoutException)
        {
            MessageBox.Show("Hết thời gian chờ VNPay trả về.", "VNPay", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            if (sender is Button btn) btn.IsEnabled = true;
        }
    }

    private void txtTopUpAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void txtTopUpAmount_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isTopUpAmountFormatting) return;
        _isTopUpAmountFormatting = true;
        try
        {
            if (sender is not TextBox tb) return;

            int caretIndex = tb.CaretIndex;
            string beforeCaret = tb.Text.Substring(0, Math.Min(caretIndex, tb.Text.Length));
            int digitsBeforeCaret = beforeCaret.Count(char.IsDigit);

            string digits = ExtractDigits(tb.Text);
            string formatted = digits.Length == 0 ? string.Empty : FormatDigitsAsVnThousands(digits);
            if (tb.Text == formatted) return;

            tb.Text = formatted;
            tb.CaretIndex = MapDigitsToCaretIndex(formatted, digitsBeforeCaret);
        }
        finally
        {
            _isTopUpAmountFormatting = false;
        }
    }

    private void TxtTopUpAmount_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (ExtractDigits(text).Length == 0) e.CancelCommand();
    }

    private static string ExtractDigits(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return Regex.Replace(text, @"\D", string.Empty);
    }

    private static string FormatDigitsAsVnThousands(string digits)
    {
        return Regex.Replace(digits, @"\B(?=(\d{3})+(?!\d))", ".");
    }

    private static int MapDigitsToCaretIndex(string formatted, int digitsBeforeCaret)
    {
        if (digitsBeforeCaret <= 0) return 0;

        int digitsSeen = 0;
        for (int i = 0; i < formatted.Length; i++)
        {
            if (!char.IsDigit(formatted[i])) continue;
            digitsSeen++;
            if (digitsSeen == digitsBeforeCaret) return i + 1;
        }

        return formatted.Length;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LoadWallet(Guid userId)
    {
        try
        {
            txtStatus.Text = "Đã tải dữ liệu.";
            var walletService = new WalletService();
            var balance = walletService.GetBalance(userId);
            txtBalance.Text = $"{balance:N0}đ";

            var txns = walletService.GetRecentTransactions(userId, 100);
            dgTransactions.ItemsSource = txns
                .Select(t => new WalletTransactionRow
                {
                    CreatedAt = t.CreatedAt,
                    Type = t.Type ?? "",
                    Direction = t.Direction ?? "",
                    Amount = t.Amount,
                    Note = t.Note ?? ""
                })
                .ToList();
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Lỗi tải ví: {ex.Message}";
            txtBalance.Text = "--";
            dgTransactions.ItemsSource = Array.Empty<WalletTransactionRow>();
        }
    }

    private sealed class WalletTransactionRow
    {
        public DateTime CreatedAt { get; init; }
        public string Type { get; init; } = "";
        public string Direction { get; init; } = "";
        public decimal Amount { get; init; }
        public string Note { get; init; } = "";

        public string DirectionText =>
            string.Equals(Direction, "Credit", StringComparison.OrdinalIgnoreCase) ? "Cộng" :
            string.Equals(Direction, "Debit", StringComparison.OrdinalIgnoreCase) ? "Trừ" : Direction;

        public string AmountText =>
            string.Equals(Direction, "Credit", StringComparison.OrdinalIgnoreCase) ? $"+{Amount:N0}đ" :
            string.Equals(Direction, "Debit", StringComparison.OrdinalIgnoreCase) ? $"-{Amount:N0}đ" : $"{Amount:N0}đ";
    }
}
