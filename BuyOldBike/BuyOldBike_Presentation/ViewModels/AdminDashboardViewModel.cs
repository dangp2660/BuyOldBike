using BuyOldBike_BLL.Features.Admin;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BuyOldBike_Presentation.ViewModels
{
    public class AdminDashboardViewModel : INotifyPropertyChanged
    {
        private readonly AdminDashboardService _service;
        private static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

        private string _totalUsersText = "0";
        public string TotalUsersText
        {
            get => _totalUsersText;
            set { _totalUsersText = value; OnPropertyChanged(); }
        }

        private string _activeListingsText = "0";
        public string ActiveListingsText
        {
            get => _activeListingsText;
            set { _activeListingsText = value; OnPropertyChanged(); }
        }

        private string _totalTransactionsText = "0";
        public string TotalTransactionsText
        {
            get => _totalTransactionsText;
            set { _totalTransactionsText = value; OnPropertyChanged(); }
        }

        private string _systemRevenueText = "0";
        public string SystemRevenueText
        {
            get => _systemRevenueText;
            set { _systemRevenueText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> RecentActivities { get; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public AdminDashboardViewModel(AdminDashboardService service)
        {
            _service = service;
        }

        public void LoadDashboard()
        {
            IsLoading = true;
            try
            {
                var stats = _service.GetDashboardStats(12);
                TotalUsersText = stats.TotalUsers.ToString("N0", NumberCulture);
                ActiveListingsText = stats.ActiveListings.ToString("N0", NumberCulture);
                TotalTransactionsText = stats.TotalTransactions.ToString("N0", NumberCulture);
                SystemRevenueText = stats.SystemRevenue.ToString("N0", NumberCulture);

                RecentActivities.Clear();
                foreach (var a in stats.RecentActivities)
                    RecentActivities.Add($"{a.CreatedAt:dd/MM} - {a.Message}");

                if (RecentActivities.Count == 0)
                    RecentActivities.Add("Chưa có hoạt động nào.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load dashboard: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

