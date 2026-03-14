using BuyOldBike_BLL.Services.Kyc;
using System;
using System.Windows;

namespace BuyOldBike_Presentation.Views
{
    public partial class EkycConfirmDialog : Window
    {
        public KycExtractResult Result { get; private set; }

        public EkycConfirmDialog(KycExtractResult initial)
        {
            InitializeComponent();

            Result = new KycExtractResult()
            {
                IdNumber = initial.IdNumber,
                FullName = initial.FullName,
                DateOfBirth = initial.DateOfBirth,
                Gender = initial.Gender,
                Nationality = initial.Nationality,
                PlaceOfOrigin = initial.PlaceOfOrigin,
                PlaceOfResidence = initial.PlaceOfResidence,
                ExpiryDate = initial.ExpiryDate
            };

            txtIdNumber.Text = Result.IdNumber ?? "";
            txtFullName.Text = Result.FullName ?? "";
            txtDateOfBirth.Text = Result.DateOfBirth ?? "";
            txtGender.Text = Result.Gender ?? "";
            txtNationality.Text = Result.Nationality ?? "";
            txtPlaceOfOrigin.Text = Result.PlaceOfOrigin ?? "";
            txtPlaceOfResidence.Text = Result.PlaceOfResidence ?? "";
            txtExpiryDate.Text = Result.ExpiryDate ?? "";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string idNumber = (txtIdNumber.Text ?? "").Trim();
            string fullName = (txtFullName.Text ?? "").Trim();
            string dob = (txtDateOfBirth.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(idNumber) ||
                string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(dob))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ CCCD, Họ tên và Ngày sinh.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (idNumber.Length > 50)
            {
                MessageBox.Show("CCCD quá dài (tối đa 50 ký tự).", "CCCD không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (fullName.Length > 255)
            {
                MessageBox.Show("Họ tên quá dài (tối đa 255 ký tự).", "Họ tên không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dob.Length > 50)
            {
                MessageBox.Show("Ngày sinh quá dài (tối đa 50 ký tự).", "Ngày sinh không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result = new KycExtractResult()
            {
                IdNumber = idNumber,
                FullName = fullName,
                DateOfBirth = dob,
                Gender = NormalizeOptional(txtGender.Text),
                Nationality = NormalizeOptional(txtNationality.Text),
                PlaceOfOrigin = NormalizeOptional(txtPlaceOfOrigin.Text),
                PlaceOfResidence = NormalizeOptional(txtPlaceOfResidence.Text),
                ExpiryDate = NormalizeOptional(txtExpiryDate.Text)
            };

            DialogResult = true;
            Close();
        }

        private static string? NormalizeOptional(string? value)
        {
            string v = (value ?? "").Trim();
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }
    }
}
