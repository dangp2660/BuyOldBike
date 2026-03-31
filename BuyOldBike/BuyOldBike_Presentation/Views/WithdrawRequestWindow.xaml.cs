using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuyOldBike_Presentation.Views;

public partial class WithdrawRequestWindow : Window
{
    private bool _isAmountFormatting;

    public string BankName { get; private set; } = "";
    public string AccountNumber { get; private set; } = "";
    public string AccountName { get; private set; } = "";
    public decimal Amount { get; private set; }

    public WithdrawRequestWindow()
    {
        InitializeComponent();
        DataObject.AddPastingHandler(txtAmount, TxtAmount_Pasting);
        cbxBankName.ItemsSource = new[]
        {
            "Vietcombank (VCB)",
            "VietinBank (CTG)",
            "BIDV (BID)",
            "Agribank",
            "Techcombank (TCB)",
            "ACB",
            "MB Bank (MBB)",
            "Sacombank (STB)",
            "VPBank (VPB)",
            "TPBank (TPB)",
            "SHB",
            "HDBank (HDB)",
            "VIB",
            "OCB",
            "MSB",
            "SeABank (SSB)"
        };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var bankName = (cbxBankName.Text ?? "").Trim();
        var accountNumber = (txtAccountNumber.Text ?? "").Trim();
        var accountName = (txtAccountName.Text ?? "").Trim();
        var digits = ExtractDigits(txtAmount.Text);

        if (string.IsNullOrWhiteSpace(bankName) ||
            string.IsNullOrWhiteSpace(accountNumber) ||
            string.IsNullOrWhiteSpace(accountName))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ ngân hàng, số tài khoản và tên chủ tài khoản.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (digits.Length == 0 ||
            !decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) ||
            amount <= 0)
        {
            MessageBox.Show("Số tiền rút không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BankName = bankName;
        AccountNumber = accountNumber;
        AccountName = accountName;
        Amount = amount;

        DialogResult = true;
        Close();
    }

    private void txtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void txtAmount_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isAmountFormatting) return;
        _isAmountFormatting = true;
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
            _isAmountFormatting = false;
        }
    }

    private void TxtAmount_Pasting(object sender, DataObjectPastingEventArgs e)
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
}
