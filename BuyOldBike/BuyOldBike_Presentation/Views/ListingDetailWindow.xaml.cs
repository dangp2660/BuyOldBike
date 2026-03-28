using BuyOldBike_BLL.Services.BicycleListWindow;
using BuyOldBike_BLL.Services.Lookups;
using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace BuyOldBike_Presentation.Views
{
    public partial class ListingDetailWindow : Window
    {
        private readonly ListingDetailViewModel _vm = new ListingDetailViewModel();
        private readonly ListingService _listingService = new ListingService();
        private readonly Guid _listingId;
        private bool _isEditMode;
        private bool _isPriceFormatting;

        public Action? OnSaved;

        public ListingDetailWindow(Guid listingId, bool editMode = false)
        {
            InitializeComponent();
            DataObject.AddPastingHandler(txtPrice_Edit, TxtPriceEdit_Pasting);
            _listingId = listingId;
            _isEditMode = editMode;

            _vm.Load(listingId);
            if (_vm.ListingBike == null)
            {
                MessageBox.Show("Không tìm thấy listing.");
                Close();
                return;
            }
            var currentUser = AppSession.CurrentUser;

            if (currentUser != null
                && currentUser.Role == "Buyer"
                && _vm.ListingBike?.SellerId != currentUser.UserId)
            {
                BtnContact.Visibility = Visibility.Visible;
            }

            if (!_isEditMode && AppSession.CurrentUser?.Role == "Buyer")
            {
                var currentUserId = AppSession.CurrentUser?.UserId;
                if (currentUserId == null || _vm.ListingBike.SellerId != currentUserId)
                {
                    try
                    {
                        new ListingBrowseService().IncrementListingViews(listingId);
                        _vm.ListingBike.Views += 1;
                    }
                    catch { }
                }
            }

            DataContext = _vm;

            if (_isEditMode)
            {
                EnterEditMode();
            }
            else
            {
                UpdateBuyerActionButtons();
            }
        }

        private void UpdateBuyerActionButtons()
        {
            if (btnDeposit != null) btnDeposit.Visibility = Visibility.Collapsed;

            if (_vm.ListingBike == null) return;

            var currentUser = AppSession.CurrentUser;
            if (currentUser == null) return;
            if (currentUser.Role != "Buyer") return;
            if (_vm.ListingBike.SellerId == currentUser.UserId) return;

            if (_vm.ListingBike.Status == BuyOldBike_DAL.Constants.StatusConstants.ListingStatus.Available)
            {
                if (btnDeposit != null) btnDeposit.Visibility = Visibility.Visible;
            }
        }
        private void BtnContact_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = AppSession.CurrentUser;
            if (currentUser == null) return;

            var sellerId = _vm.ListingBike?.SellerId;
            if (sellerId == null) return;

            // Không cho chat với chính mình
            if (currentUser.UserId == sellerId) return;

            var chatWindow = new ChatWindow(
                _listingId,
                currentUser.UserId,
                sellerId.Value);

            chatWindow.Owner = this; 
            chatWindow.Show();
        }

        private void EnterEditMode()
        {
            if (btnDeposit != null) btnDeposit.Visibility = Visibility.Collapsed;

            // Show editable controls
            tbTitle.Visibility = Visibility.Collapsed;
            tbDesc.Visibility = Visibility.Collapsed;
            tbPrice.Visibility = Visibility.Collapsed;
            tbFrame.Visibility = Visibility.Collapsed;
            tbUsage.Visibility = Visibility.Collapsed;
            tbBrandType.Visibility = Visibility.Collapsed;

            txtTitle_Edit.Visibility = Visibility.Visible;
            txtDesc_Edit.Visibility = Visibility.Visible;
            txtPrice_Edit.Visibility = Visibility.Visible;
            cbxFrame_Edit.Visibility = Visibility.Visible;
            txtUsage_Edit.Visibility = Visibility.Visible;
            cbxBrand_Edit.Visibility = Visibility.Visible;
            cbxType_Edit.Visibility = Visibility.Visible;
            btnSave.Visibility = Visibility.Visible;
            btnAddImage.Visibility = Visibility.Visible;

            // populate editable fields
            txtTitle_Edit.Text = _vm.ListingBike?.Title ?? string.Empty;
            txtDesc_Edit.Text = _vm.ListingBike?.Description ?? string.Empty;
            var priceValue = _vm.ListingBike?.Price ?? 0m;
            txtPrice_Edit.Text = FormatDigitsAsVnThousands(decimal.Truncate(priceValue).ToString(CultureInfo.InvariantCulture));
            txtUsage_Edit.Text = _vm.ListingBike?.UsageDuration?.ToString() ?? string.Empty;
            var frameValue = (_vm.ListingBike?.FrameNumber ?? string.Empty).Trim();
            for (int i = 0; i < cbxFrame_Edit.Items.Count; i++)
            {
                if (cbxFrame_Edit.Items[i] is not ComboBoxItem item) continue;
                if (string.Equals(item.Content?.ToString(), frameValue, StringComparison.OrdinalIgnoreCase))
                {
                    cbxFrame_Edit.SelectedIndex = i;
                    break;
                }
            }

            var lookupService = new LookupService();

            // load brands into cbxBrand_Edit
            var brands = lookupService.GetBrands();
            cbxBrand_Edit.ItemsSource = brands;
            cbxBrand_Edit.SelectedValuePath = "BrandId";
            cbxBrand_Edit.DisplayMemberPath = "BrandName";
            if (_vm.ListingBike?.BrandId != null)
            {
                cbxBrand_Edit.SelectedValue = _vm.ListingBike.BrandId;
            }

            // load bike types into cbxType_Edit
            var types = lookupService.GetBikeTypes();
            cbxType_Edit.ItemsSource = types;
            cbxType_Edit.SelectedValuePath = "BikeTypeId";
            cbxType_Edit.DisplayMemberPath = "Name";
            if (_vm.ListingBike?.BikeTypeId != null)
            {
                cbxType_Edit.SelectedValue = _vm.ListingBike.BikeTypeId;
            }
        }

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            var paths = new List<string>();
            var previews = new List<BitmapImage>();
            foreach (var f in dialog.FileNames)
            {
                try
                {
                    string dest = Path.Combine(Path.GetTempPath(), $"postimg_{Guid.NewGuid()}{Path.GetExtension(f)}");
                    File.Copy(f, dest, true);
                    paths.Add(dest);

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(dest);
                    bmp.EndInit();
                    bmp.Freeze();
                    previews.Add(bmp);
                }
                catch { }
            }

            if (paths.Any())
            {
                try
                {
                    _listingService.AddImagesToListing(_listingId, paths);
                    foreach (var bmp in previews) _vm.Images.Add(bmp);
                    MessageBox.Show($"Đã thêm {paths.Count} ảnh.");
                }
                catch (Exception ex)
                {
                    foreach (var p in paths)
                    {
                        try { File.Delete(p); } catch { }
                    }
                    MessageBox.Show(ex.Message, "Không thể thêm ảnh", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.ListingBike == null) return;

                if (!TryParsePriceText(txtPrice_Edit.Text, out decimal price))
                {
                    MessageBox.Show("Giá không hợp lệ.");
                    return;
                }

                _vm.ListingBike.Title = txtTitle_Edit.Text.Trim();
                _vm.ListingBike.Description = txtDesc_Edit.Text.Trim();
                _vm.ListingBike.Price = price;
                _vm.ListingBike.FrameNumber = (cbxFrame_Edit.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (int.TryParse(txtUsage_Edit.Text, out int months)) _vm.ListingBike.UsageDuration = months;
                if (cbxBrand_Edit.SelectedValue is int bid) _vm.ListingBike.BrandId = bid;
                if (cbxType_Edit.SelectedValue is int tid) _vm.ListingBike.BikeTypeId = tid;

                // set status back to pending inspection for re-approval
                _vm.ListingBike.Status = BuyOldBike_DAL.Constants.StatusConstants.ListingStatus.Pending_Inspection;

                // call listing service to update
                _listingService.UpdateListing(_vm.ListingBike);

                MessageBox.Show("Cập nhật bài đăng thành công.");

                OnSaved?.Invoke();

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu bài đăng: {ex.Message}");
            }
        }

        private void txtPrice_Edit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtPrice_Edit_TextChanged(object sender, TextChangedEventArgs e)
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

        private void TxtPriceEdit_Pasting(object sender, DataObjectPastingEventArgs e)
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

        private void BtnDeposit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AppSession.CurrentUser == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập để thực hiện chức năng này.");
                    return;
                }

                if (_vm.ListingBike == null) return;

                var addressDialog = new DeliveryAddressDialog(AppSession.CurrentUser.UserId)
                {
                    Owner = this
                };
                if (addressDialog.ShowDialog() != true || addressDialog.ResultAddress == null) return;

                var result = MessageBox.Show($"Bạn có chắc chắn muốn đặt cọc 20% cho xe này bằng số dư trong ví?", "Xác nhận", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;

                var depositService = new BuyOldBike_BLL.Features.Payments.DepositService();
                depositService.PlaceDepositWithWallet(AppSession.CurrentUser.UserId, _vm.ListingBike.ListingId, addressDialog.ResultAddress);
                
                MessageBox.Show("Đặt cọc thành công!");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đặt cọc: {ex.Message}");
            }
        }

        
    }
}
