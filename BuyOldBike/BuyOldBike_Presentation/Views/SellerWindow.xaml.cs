using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_BLL.Features.Payments.Wallet;
using BuyOldBike_BLL.Services.Lookups;
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
        private SellerChatViewModel? _chatVm;

        public SellerWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            DataObject.AddPastingHandler(txtPrice, TxtPrice_Pasting);
            DataObject.AddPastingHandler(txtUsage, TxtUsage_Pasting);
            DataObject.AddPastingHandler(txtSellerTopUpAmount, TxtSellerTopUpAmount_Pasting);
            if (!RoleNavigator.EnsureRole(this, RoleConstants.Seller)) return;
            Load();
            var currentUser = AppSession.CurrentUser;
            if (currentUser != null)
            {
                _vm.LoadUnreadMessageCount(currentUser.UserId);
                _chatVm = new SellerChatViewModel(currentUser.UserId);
                LbConversations.ItemsSource = _chatVm.Conversations;
            }
        }

        private void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            // Publish directly from form in this tab now
            if (AppSession.CurrentUser == null)
            {
                MessageBox.Show("Không có người dùng đăng nhập.");
                return;
            }

            try
            {
                string title = txtTitle.Text?.Trim() ?? string.Empty;
                string desc = txtDecription.Text?.Trim() ?? string.Empty;
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(title)) errors.Add("- Vui lòng nhập tiêu đề.");
                if (string.IsNullOrWhiteSpace(desc)) errors.Add("- Vui lòng nhập mô tả.");

                if (!TryParsePriceText(txtPrice.Text, out decimal price) || price <= 0)
                    errors.Add("- Giá phải là số lớn hơn 0.");

                if (string.IsNullOrWhiteSpace(txtUsage.Text))
                {
                    errors.Add("- Vui lòng nhập thời gian sử dụng (tháng).");
                }
                else if (!int.TryParse(txtUsage.Text, out int months) || months < 0)
                {
                    errors.Add("- Thời gian sử dụng phải là số nguyên không âm.");
                }

                var brandId = cbxBrand.SelectedValue as int?;
                if (brandId == null) errors.Add("- Vui lòng chọn Brand.");

                var bikeTypeId = cbxBikeType.SelectedValue as int?;
                if (bikeTypeId == null) errors.Add("- Vui lòng chọn Bike Type.");

                var frameNumber = (cbxFrame.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (string.IsNullOrWhiteSpace(frameNumber)) errors.Add("- Vui lòng chọn Frame.");

                if (_vm.SelectedImagePreviews.Count == 0) errors.Add("- Vui lòng upload ít nhất 1 ảnh.");

                if (errors.Count > 0)
                {
                    MessageBox.Show(string.Join("\n", errors), "Thiếu hoặc sai thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int.TryParse(txtUsage.Text, out int monthsParsed);

                var listing = new Listing
                {
                    Title = title,
                    Description = desc,
                    Price = price,
                    Status = StatusConstants.ListingStatus.Pending_Inspection,
                    FrameNumber = frameNumber,
                    UsageDuration = monthsParsed,
                    SellerId = AppSession.CurrentUser.UserId,
                    BrandId = brandId,
                    BikeTypeId = bikeTypeId
                };

                List<string> imagePaths = new List<string>();
                foreach (var bmp in _vm.SelectedImagePreviews)
                {
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
                    var postingFee = Math.Round(price * 0.05m, 0, MidpointRounding.AwayFromZero);
                    var walletService = new WalletService();
                    var balance = walletService.GetBalance(AppSession.CurrentUser.UserId);

                    var confirm = MessageBox.Show(
                        $"Phí đăng bài: {postingFee:N0}đ (5% giá bán)\nSố dư ví: {balance:N0}đ\nBạn có muốn tiếp tục đăng bài?",
                        "Xác nhận phí đăng bài",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;

                    _vm.CreateNewPost(listing, imagePaths);
                    MessageBox.Show("Đã gửi bài đăng để kiểm định.");
                    LoadSellerWallet();
                }

                ClearForm();
                LoadSellerListings();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể đăng bài", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch
            {
                MessageBox.Show("Có lỗi xảy ra. Vui lòng thử lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtUsage_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        private void TxtUsage_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!Regex.IsMatch(text, @"^\d+$")) e.CancelCommand();
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
            var lookupService = new LookupService();
            var brands = lookupService.GetBrands();
            cbxBrand.ItemsSource = brands;
            cbxBrand.SelectedValuePath = "BrandId";
            cbxBrand.DisplayMemberPath = "BrandName";
            var types = lookupService.GetBikeTypes();
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
            cbxFrame.SelectedIndex = 0;
            txtUsage.Text = string.Empty;
            _vm.SelectedImagePreviews.Clear();
            lblImageCount.Content = string.Empty;
            _editingListingId = null;
        }

        private void BtnEditListing_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerListingRow row)) return;
            if (!row.IsPending)
            {
                MessageBox.Show("Chỉ có thể chỉnh sửa bài đăng trước khi kiểm định (trạng thái 'Pending_Inspection').", "Không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

                try
                {
                    _vm.HideListing(row.ListingId);
                    row.Status = StatusConstants.ListingStatus.Hidden;
                    MessageBox.Show("Đã ẩn bài đăng.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể ẩn", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            if (row.IsHidden)
            {
                var confirm = MessageBox.Show("Bạn có muốn hiển thị lại bài đăng này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    _vm.UnhideListing(row.ListingId);
                    row.Status = StatusConstants.ListingStatus.Available;
                    MessageBox.Show("Đã hiển thị lại bài đăng.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể hiển thị", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            MessageBox.Show("Chỉ có thể ẩn bài đăng khi trạng thái là 'Available' và chỉ có thể hiển thị lại khi đang 'Hidden'.", "Không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnDeleteListing_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ViewModels.SellerListingRow row)) return;
            if (!row.IsAvailable)
            {
                MessageBox.Show("Chỉ có thể xóa bài đăng khi trạng thái là 'Available'.", "Không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa bài đăng này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _vm.DeleteListing(row.ListingId);
                MessageBox.Show("Đã xóa bài đăng (đánh dấu deleted). ");
                LoadSellerListings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        //chat
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl) return;
            if (TabMessages?.IsSelected == true)
                _chatVm?.LoadConversations();
            if (TabSellerReviews?.IsSelected == true && AppSession.CurrentUser != null)
                SellerReviewsControl?.LoadSellerReviews(AppSession.CurrentUser.UserId);
            RefreshUnreadMessageCount();
        }
        private void LbConversations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_chatVm == null) return;
            if (LbConversations.SelectedItem is not Message selected) return;

            _chatVm.SelectedConversation = selected;
            IcSellerMessages.ItemsSource = _chatVm.CurrentMessages;

            // Cập nhật header
            var buyerEmail = selected.SenderId == _chatVm.SellerId
                ? selected.Receiver?.Email
                : selected.Sender?.Email;
            TxtChatHeader.Text = $"{buyerEmail} — {selected.Listing?.Title}";

            SellerChatScroll.ScrollToBottom();
            RefreshUnreadMessageCount();
        }
        private void BtnRefreshConversations_Click(object sender, RoutedEventArgs e)
        {
            _chatVm?.LoadConversations();
            RefreshUnreadMessageCount();
        }

        private void BtnSellerSendChat_Click(object sender, RoutedEventArgs e)
        {
            if (_chatVm == null) return;
            _chatVm.InputText = TxtSellerChatInput.Text;
            _chatVm.Send();
            TxtSellerChatInput.Text = string.Empty;
            SellerChatScroll.ScrollToBottom();
            RefreshUnreadMessageCount();
        }

        private void BtnRefreshSellerChat_Click(object sender, RoutedEventArgs e)
        {
            _chatVm?.Refresh();
            SellerChatScroll.ScrollToBottom();
            RefreshUnreadMessageCount();
        }

        private void RefreshUnreadMessageCount()
        {
            if (AppSession.CurrentUser == null) return;
            _vm.LoadUnreadMessageCount(AppSession.CurrentUser.UserId);
        }
    }
}
