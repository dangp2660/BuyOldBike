using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_BLL.Features.Payments.Wallet;
using BuyOldBike_Presentation.Payments;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace BuyOldBike_Presentation.Views
{
    public partial class SellerWindow : Window
    {
        private readonly SellerWindowViewModel _vm = new SellerWindowViewModel();
        private Guid? _editingListingId;
        private bool _isPriceFormatting;
        private bool _isSellerTopUpAmountFormatting;

        public SellerWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            DataObject.AddPastingHandler(txtPrice, TxtPrice_Pasting);
            DataObject.AddPastingHandler(txtSellerTopUpAmount, TxtSellerTopUpAmount_Pasting);
            if (!RoleNavigator.EnsureRole(this, RoleConstants.Seller)) return;
            Load();
        }

        private void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            // Publish directly from form in this tab now
            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Không có người dùng đăng nhập.");
                return;
            }

            string title = txtTitle.Text?.Trim() ?? string.Empty;
            string desc = txtDecription.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParsePriceText(txtPrice.Text, out decimal price))
            {
                MessageBox.Show("Giá không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var listing = new Listing
            {
                Title = title,
                Description = desc,
                Price = price,
                Status = StatusConstants.ListingStatus.Pending_Inspection,
                FrameNumber = tbFrameNum.Text,
                UsageDuration = int.TryParse(txtUsage.Text, out int months) ? months : null,
                SellerId = AppSession.CurrentUser.UserId,
                BrandId = cbxBrand.SelectedValue as int?,
                BikeTypeId = cbxBikeType.SelectedValue as int?
            };

            // images: use SelectedImagePreviews for now as placeholders; real app should upload and store urls
            List<string> imagePaths = new List<string>();
            foreach (var bmp in _vm.SelectedImagePreviews)
            {
                // write to temp file
                string path = Path.Combine(Path.GetTempPath(), $"post_{Guid.NewGuid()}.png");
                SaveBitmapToFile(bmp, path);
                imagePaths.Add(path);
            }

            if (_editingListingId.HasValue)
            {
                listing.ListingId = _editingListingId.Value;
                listing.Status = StatusConstants.ListingStatus.Pending_Inspection;
                _vm.UpdateListing(listing);
                MessageBox.Show("Cập nhật bài đăng thành công.");
                _editingListingId = null;
            }
            else
            {
                _vm.CreateNewPost(listing, imagePaths);
                MessageBox.Show("Đã gửi bài đăng để kiểm định.");
            }

            ClearForm();
            LoadSellerListings();
        }

        private void txtPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPriceFormatting) return;
            _isPriceFormatting = true;
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
                _isPriceFormatting = false;
            }
        }

        private void txtSellerTopUpAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtSellerTopUpAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSellerTopUpAmountFormatting) return;
            _isSellerTopUpAmountFormatting = true;
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
                _isSellerTopUpAmountFormatting = false;
            }
        }

        private void TxtSellerTopUpAmount_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (ExtractDigits(text).Length == 0) e.CancelCommand();
        }

        private void TxtPrice_Pasting(object sender, DataObjectPastingEventArgs e)
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

        private static bool TryParsePriceText(string? text, out decimal price)
        {
            string digits = ExtractDigits(text);
            if (digits.Length == 0)
            {
                price = 0;
                return false;
            }

            return decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out price);
        }

        private void SaveBitmapToFile(BitmapImage bmp, string path)
        {
            try
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using var fs = new FileStream(path, FileMode.Create);
                encoder.Save(fs);
            }
            catch
            {
                // ignore write errors for temp preview
            }
        }

        private void btnUpImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            foreach (var f in dialog.FileNames)
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(f);
                    bmp.EndInit();
                    bmp.Freeze();
                    _vm.SelectedImagePreviews.Add(bmp);
                }
                catch { }
            }

            lblImageCount.Content = _vm.SelectedImagePreviews.Count + " ảnh";
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutManager.Logout(this);
        }

        private void BtnOpenWallet_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAuthenticated) return;
            var win = new WalletWindow();
            win.Owner = this;
            win.ShowDialog();
            LoadSellerWallet();
        }

        public void Load()
        {
            BuyOldBikeContext db = new BuyOldBikeContext();
            var brands = db.Brands.ToList();
            cbxBrand.ItemsSource = brands;
            cbxBrand.SelectedValuePath = "BrandId";
            cbxBrand.DisplayMemberPath = "BrandName";
            var types = db.Types.ToList();
            cbxBikeType.ItemsSource = types;
            cbxBikeType.SelectedValuePath = "BikeTypeId";
            cbxBikeType.DisplayMemberPath = "Name";
            if (brands.Any()) cbxBrand.SelectedIndex = 0;
            if (types.Any()) cbxBikeType.SelectedIndex = 0;
            LoadSellerListings();
            LoadSellerOrders();
            LoadSellerWallet();
        }

        private void LoadSellerListings()
        {
            if (AppSession.CurrentUser == null) return;
            _vm.LoadSellerListings(AppSession.CurrentUser.UserId);
        }

        private void LoadSellerOrders()
        {
            if (AppSession.CurrentUser == null) return;
            _vm.LoadSellerOrders(AppSession.CurrentUser.UserId);
        }

        private void LoadSellerWallet()
        {
            if (AppSession.CurrentUser == null) return;
            try
            {
                var walletService = new WalletService();
                var balance = walletService.GetBalance(AppSession.CurrentUser.UserId);
                txtSellerWalletBalance.Text = $"{balance:N0}đ";

                var txns = walletService.GetRecentTransactions(AppSession.CurrentUser.UserId, 50);
                dgSellerWalletTransactions.ItemsSource = txns
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
            catch
            {
                txtSellerWalletBalance.Text = "--";
                dgSellerWalletTransactions.ItemsSource = Array.Empty<WalletTransactionRow>();
            }
        }

        private void ClearForm()
        {
            txtTitle.Text = string.Empty;
            txtPrice.Text = string.Empty;
            txtDecription.Text = string.Empty;
            cbxBrand.SelectedIndex = 0;
            tbFrameNum.Text = string.Empty;
            txtUsage.Text = string.Empty;
            _vm.SelectedImagePreviews.Clear();
            lblImageCount.Content = string.Empty;
            _editingListingId = null;
        }

        private void BtnEditListing_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerListingRow row)) return;

            // Open listing detail window in edit mode
            var win = new ListingDetailWindow(row.ListingId, editMode: true) 
            { 
                Owner = this,
                OnSaved = () => LoadSellerListings()
            };
            win.ShowDialog();
        }

        private void BtnToggleHide_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerListingRow row)) return;

            // Only allow hide when listing is Available; allow unhide when Hidden
            if (row.IsAvailable)
            {
                var confirm = MessageBox.Show("Bạn có muốn ẩn bài đăng này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                _vm.HideListing(row.ListingId);
                row.Status = StatusConstants.ListingStatus.Hidden;
                MessageBox.Show("Đã ẩn bài đăng.");
                return;
            }

            if (row.IsHidden)
            {
                var confirm = MessageBox.Show("Bạn có muốn hiển thị lại bài đăng này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                _vm.UnhideListing(row.ListingId);
                row.Status = StatusConstants.ListingStatus.Available;
                MessageBox.Show("Đã hiển thị lại bài đăng.");
                return;
            }

            MessageBox.Show("Chỉ có thể ẩn bài đăng khi trạng thái là 'Available' và chỉ có thể hiển thị lại khi đang 'Hidden'.", "Không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnDeleteListing_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerListingRow row)) return;

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa bài đăng này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            _vm.DeleteListing(row.ListingId);
            MessageBox.Show("Đã xóa bài đăng (đánh dấu deleted). ");
            LoadSellerListings();
        }

        private void BtnAcceptOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerOrderRow row)) return;

            var confirm = MessageBox.Show("Xác nhận chấp nhận đơn hàng này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            _vm.UpdateOrderStatus(row.OrderId, "Accepted");
            MessageBox.Show("Đã chấp nhận đơn hàng.");
            LoadSellerOrders();
        }

        private void BtnRejectOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerOrderRow row)) return;

            var confirm = MessageBox.Show("Xác nhận từ chối đơn hàng này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            _vm.UpdateOrderStatus(row.OrderId, "Rejected");
            MessageBox.Show("Đã từ chối đơn hàng.");
            LoadSellerOrders();
        }

        private void BtnCompleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerOrderRow row)) return;

            var confirm = MessageBox.Show("Đánh dấu đơn hàng này là hoàn thành?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            _vm.UpdateOrderStatus(row.OrderId, "Completed");
            MessageBox.Show("Đã đánh dấu hoàn thành.");
            LoadSellerOrders();
        }

        private async void BtnSellerTopUp_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAuthenticated || AppSession.CurrentUser == null)
            {
                MessageBox.Show("Bạn cần đăng nhập để nạp tiền.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var digits = new string((txtSellerTopUpAmount.Text ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length == 0 || !decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
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
                LoadSellerWallet();

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
}
