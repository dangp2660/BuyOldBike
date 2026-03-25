using BuyOldBike_BLL.Services.Kyc;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
namespace BuyOldBike_Presentation.Views
{


    public partial class RegisterWindow : Window
    {
        private byte[]? _front;
        private byte[]? _back;
        private byte[]? _selfie;
        private KycExtractResult? _extractedInfo;

        private readonly EkycService _ekycService = new EkycService(new EkycOcrService(Path.Combine(AppContext.BaseDirectory, "tessdata")));
        public RegisterWindow()
        {
            InitializeComponent();
            btnSaveEkyc.IsEnabled = false;
            btnRegister.IsEnabled = false;
        }

        private void btnFrontImge_Click(object sender, RoutedEventArgs e) => ChoiceImage(bytes =>
        {
            _front = bytes;
            imgFrontPreview.Source = ToImage(bytes);
        });

        private void btnBackImg_Click(object sender, RoutedEventArgs e) => ChoiceImage(bytes =>
        {
            _back = bytes;
            imgBackPreview.Source = ToImage(bytes);
        });

        private void btnSelfiImg_Click(object sender, RoutedEventArgs e) => ChoiceImage(bytes =>
        {
            _selfie = bytes;
            imgSelfiePreview.Source = ToImage(bytes);
        });

        private void ChoiceImage(Action<byte[]> selected)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true) return;

            byte[] bytes = File.ReadAllBytes(dialog.FileName);
            selected(bytes);
            btnSaveEkyc.IsEnabled = _front != null && _back != null && _selfie != null;
        }

        private static BitmapImage ToImage(byte[] bytes)
        {
            var bmp = new BitmapImage();
            var ms = new MemoryStream(bytes);
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private void btnSaveEkyc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_front == null || _back == null || _selfie == null) return;

                _extractedInfo = _ekycService.ExtractKycInfo(_front, _back, _selfie);
                string gender = string.IsNullOrWhiteSpace(_extractedInfo.Gender) ? "N/A" : _extractedInfo.Gender;
                string nationality = string.IsNullOrWhiteSpace(_extractedInfo.Nationality) ? "N/A" : _extractedInfo.Nationality;
                string placeOfOrigin = string.IsNullOrWhiteSpace(_extractedInfo.PlaceOfOrigin) ? "N/A" : _extractedInfo.PlaceOfOrigin;
                string placeOfResidence = string.IsNullOrWhiteSpace(_extractedInfo.PlaceOfResidence) ? "N/A" : _extractedInfo.PlaceOfResidence;
                string expiryDate = string.IsNullOrWhiteSpace(_extractedInfo.ExpiryDate) ? "N/A" : _extractedInfo.ExpiryDate;

                placeOfResidence = placeOfResidence == "N/A"
                    ? placeOfResidence
                    : placeOfResidence.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine).Replace(", ", "," + Environment.NewLine);

                string message =
                    $"CCCD: {_extractedInfo.IdNumber}\n" +
                    $"Họ tên: {_extractedInfo.FullName}\n" +
                    $"Ngày sinh: {_extractedInfo.DateOfBirth}\n" +
                    $"Giới tính: {gender}\n" +
                    $"Quốc tịch: {nationality}\n" +
                    $"Quê quán: {placeOfOrigin}\n" +
                    $"Nơi thường trú: {placeOfResidence}\n" +
                    $"Giá trị đến: {expiryDate}";

                MessageBox.Show(message, "eKYC info", MessageBoxButton.OK, MessageBoxImage.Information);
                btnRegister.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _extractedInfo = null;
                var root = ex.GetBaseException();
                MessageBox.Show($"Lỗi trích xuất thông tin từ ảnh: {root.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                btnRegister.IsEnabled = false;
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (_extractedInfo == null || _front == null || _back == null || _selfie == null)
            {
                MessageBox.Show("Vui lòng hoàn thành đầy đủ thông tin và trích xuất eKYC trước khi đăng ký.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string email = txtEmail.Text.Trim() ?? "";
            string phone = txtPhone.Text.Trim() ?? "";
            string password = txtPassword.Password.Trim() ?? "";
            string confirmPassword = txtConfirmPassword.Password.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ email, số điện thoại và mật khẩu.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!email.Contains("@") || !email.Contains("."))
            {
                MessageBox.Show("Vui lòng nhập email hợp lệ.", "Email không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (email.Length > 255)
            {
                MessageBox.Show("Email quá dài (tối đa 255 ký tự).", "Email không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (phone.Length > 20)
            {
                MessageBox.Show("Số điện thoại quá dài (tối đa 20 ký tự).", "Số điện thoại không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (password.Length > 255)
            {
                MessageBox.Show("Mật khẩu quá dài (tối đa 255 ký tự).", "Mật khẩu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(password != confirmPassword)
            {
                MessageBox.Show("Mật khẩu và xác nhận mật khẩu không khớp.", "Lỗi mật khẩu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmDialog = new EkycConfirmDialog(_extractedInfo)
            {
                Owner = this
            };
            bool confirmed = confirmDialog.ShowDialog() == true;
            if (!confirmed) return;

            _extractedInfo = confirmDialog.Result;

            try
            {
                bool ok = _ekycService.RegisterBuyer(email, phone, password, _extractedInfo, _front, _back, _selfie);
                MessageBox.Show(ok ? "Đăng ký thành công!" : "Đăng ký thất bại. Email có thể đã được sử dụng.", ok ? "Thành công" : "Thất bại", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
                if (ok)
                {
                    this.Close();
                    LoginWindow lgWindow = new LoginWindow();
                    lgWindow.Show();
                }
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                MessageBox.Show($"Đăng ký thất bại: {root.Message}", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


    }
}
