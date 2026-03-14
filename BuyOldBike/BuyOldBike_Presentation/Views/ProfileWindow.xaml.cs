using BuyOldBike_BLL.Services.Kyc;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.State;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.Views
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var user = AppSession.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Bạn cần đăng nhập để xem hồ sơ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            txtEmail.Text = user.Email;
            txtPhone.Text = user.PhoneNumber;
            txtRole.Text = user.Role;

            var kycService = new KycProfileService();
            var profile = kycService.GetLatestProfile(user.UserId);
            if (profile == null)
            {
                txtStatus.Text = "Tài khoản này chưa có hồ sơ eKYC.";
                kycInfoSection.Visibility = Visibility.Collapsed;
                kycImagesSection.Visibility = Visibility.Collapsed;
                return;
            }

            txtStatus.Text = "Đã tải hồ sơ eKYC.";
            FillKycInfo(profile);
            FillKycImages(profile);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FillKycInfo(KycProfile profile)
        {
            txtIdNumber.Text = profile.IdNumber ?? "";
            txtFullName.Text = profile.FullName ?? "";
            txtDob.Text = profile.DateOfBirth ?? "";
            txtGender.Text = profile.Gender ?? "";
            txtNationality.Text = profile.Nationality ?? "";
            txtOrigin.Text = profile.PlaceOfOrigin ?? "";
            txtResidence.Text = profile.PlaceOfResidence ?? "";
            txtExpiry.Text = profile.ExpiryDate ?? "";
            txtVerifiedAt.Text = profile.VerifiedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "";
        }

        private void FillKycImages(KycProfile profile)
        {
            var images = profile.KycImages?.ToList() ?? [];

            imgFront.Source = ToBitmapImage(images.FirstOrDefault(i => i.ImageType == "Front")?.ImageData);
            imgBack.Source = ToBitmapImage(images.FirstOrDefault(i => i.ImageType == "Back")?.ImageData);
            imgSelfie.Source = ToBitmapImage(images.FirstOrDefault(i => i.ImageType == "Selfie")?.ImageData);
        }

        private static BitmapImage? ToBitmapImage(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            using var ms = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
