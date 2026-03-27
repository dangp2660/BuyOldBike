using BuyOldBike_BLL.Features.Payments;
using BuyOldBike_DAL.Entities;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace BuyOldBike_Presentation.Views
{
    public partial class DeliveryAddressDialog : Window
    {
        private static readonly HttpClient _http = new HttpClient();
        private bool _isLoadingProvince;
        private bool _isLoadingDistrict;
        private string _prefillProvince = string.Empty;
        private string _prefillDistrict = string.Empty;
        private string _prefillWard = string.Empty;

        public DeliveryAddressInfo? ResultAddress { get; private set; }

        public DeliveryAddressDialog(Guid? userId)
        {
            InitializeComponent();
            Loaded += DeliveryAddressDialog_Loaded;
            PrefillFromUserAddress(userId);
        }

        private async void DeliveryAddressDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProvincesAsync();
        }

        private void PrefillFromUserAddress(Guid? userId)
        {
            if (userId == null) return;

            try
            {
                using var db = new BuyOldBikeContext();
                var user = db.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
                {
                    txtPhoneNumber.Text = user.PhoneNumber;
                }

                var addr = db.Addresses.FirstOrDefault(a => a.UserId == userId);
                if (addr != null)
                {
                    txtFullName.Text = addr.FullName ?? string.Empty;
                    if (string.IsNullOrEmpty(txtPhoneNumber.Text))
                    {
                        txtPhoneNumber.Text = addr.PhoneNumber ?? string.Empty;
                    }
                    _prefillProvince = (addr.Province ?? addr.City ?? string.Empty).Trim();
                    _prefillDistrict = (addr.District ?? string.Empty).Trim();
                    _prefillWard = (addr.Ward ?? string.Empty).Trim();
                    cbxProvince.Text = _prefillProvince;
                    cbxDistrict.Text = _prefillDistrict;
                    cbxWard.Text = _prefillWard;
                    txtDetail.Text = addr.Detail ?? string.Empty;
                }
            }
            catch { }
        }

        private async Task LoadProvincesAsync()
        {
            if (_isLoadingProvince) return;
            _isLoadingProvince = true;
            ProvinceDto? toSelect = null;
            try
            {
                var url = "https://provinces.open-api.vn/api/p/";
                var json = await _http.GetStringAsync(url);
                var provinces = JsonSerializer.Deserialize<ProvinceDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Array.Empty<ProvinceDto>();
                var sorted = provinces.OrderBy(p => p.Name).ToArray();
                cbxProvince.ItemsSource = sorted;
                if (!string.IsNullOrWhiteSpace(_prefillProvince))
                {
                    toSelect = sorted.FirstOrDefault(p => string.Equals(p.Name, _prefillProvince, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            catch
            {
            }
            finally
            {
                _isLoadingProvince = false;
            }
            if (toSelect != null)
            {
                cbxProvince.SelectedItem = toSelect;
            }
        }

        private async void CbxProvince_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isLoadingProvince) return;

            cbxDistrict.ItemsSource = null;
            cbxWard.ItemsSource = null;
            cbxDistrict.IsEnabled = false;
            cbxWard.IsEnabled = false;
            cbxDistrict.Text = string.Empty;
            cbxWard.Text = string.Empty;

            if (cbxProvince.SelectedItem is not ProvinceDto p) return;
            await LoadDistrictsAsync(p.Code);
        }

        private async Task LoadDistrictsAsync(int provinceCode)
        {
            if (_isLoadingDistrict) return;
            _isLoadingDistrict = true;
            DistrictDto? toSelect = null;
            try
            {
                var url = $"https://provinces.open-api.vn/api/p/{provinceCode}?depth=2";
                var json = await _http.GetStringAsync(url);
                var province = JsonSerializer.Deserialize<ProvinceWithDistrictsDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var districts = province?.Districts ?? Array.Empty<DistrictDto>();
                var sorted = districts.OrderBy(d => d.Name).ToArray();
                cbxDistrict.ItemsSource = sorted;
                cbxDistrict.IsEnabled = true;
                if (!string.IsNullOrWhiteSpace(_prefillDistrict))
                {
                    toSelect = sorted.FirstOrDefault(d => string.Equals(d.Name, _prefillDistrict, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            catch
            {
            }
            finally
            {
                _isLoadingDistrict = false;
            }
            if (toSelect != null)
            {
                cbxDistrict.SelectedItem = toSelect;
            }
        }

        private async void CbxDistrict_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isLoadingDistrict) return;

            cbxWard.ItemsSource = null;
            cbxWard.IsEnabled = false;
            cbxWard.Text = string.Empty;

            if (cbxDistrict.SelectedItem is not DistrictDto d) return;
            await LoadWardsAsync(d.Code);
        }

        private async Task LoadWardsAsync(int districtCode)
        {
            try
            {
                var url = $"https://provinces.open-api.vn/api/d/{districtCode}?depth=2";
                var json = await _http.GetStringAsync(url);
                var district = JsonSerializer.Deserialize<DistrictWithWardsDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var wards = district?.Wards ?? Array.Empty<WardDto>();
                var sorted = wards.OrderBy(w => w.Name).ToArray();
                cbxWard.ItemsSource = sorted;
                cbxWard.IsEnabled = true;
                if (!string.IsNullOrWhiteSpace(_prefillWard))
                {
                    var toSelect = sorted.FirstOrDefault(w => string.Equals(w.Name, _prefillWard, StringComparison.CurrentCultureIgnoreCase));
                    if (toSelect != null)
                    {
                        cbxWard.SelectedItem = toSelect;
                    }
                }
            }
            catch
            {
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var province = (cbxProvince.SelectedItem as ProvinceDto)?.Name ?? (cbxProvince.Text ?? string.Empty).Trim();
            var district = (cbxDistrict.SelectedItem as DistrictDto)?.Name ?? (cbxDistrict.Text ?? string.Empty).Trim();
            var ward = (cbxWard.SelectedItem as WardDto)?.Name ?? (cbxWard.Text ?? string.Empty).Trim();
            var detail = (txtDetail.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(province) ||
                string.IsNullOrWhiteSpace(district) ||
                string.IsNullOrWhiteSpace(ward) ||
                string.IsNullOrWhiteSpace(detail))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tỉnh/thành phố, quận/huyện, phường/xã và địa chỉ chi tiết.");
                return;
            }

            ResultAddress = new DeliveryAddressInfo
            {
                FullName = (txtFullName.Text ?? string.Empty).Trim(),
                PhoneNumber = (txtPhoneNumber.Text ?? string.Empty).Trim(),
                Province = province,
                District = district,
                Ward = ward,
                Detail = detail
            };

            DialogResult = true;
            Close();
        }

        private class ProvinceDto
        {
            public int Code { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class DistrictDto
        {
            public int Code { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class WardDto
        {
            public int Code { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class ProvinceWithDistrictsDto
        {
            public DistrictDto[] Districts { get; set; } = Array.Empty<DistrictDto>();
        }

        private class DistrictWithWardsDto
        {
            public WardDto[] Wards { get; set; } = Array.Empty<WardDto>();
        }
    }
}
