using BuyOldBike_BLL.Services.Listings;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BuyOldBike_Presentation.ViewModels
{
    public class ListingModerationViewModel : INotifyPropertyChanged
    {
        private readonly ListingModerationService _service;

        public ObservableCollection<Listing> Listings { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        private string _selectedStatus = "All status";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ListingModerationViewModel(ListingModerationService service)
        {
            _service = service;
        }

        public void LoadListings()
        {
            IsLoading = true;
            try
            {
                var list = _service.GetListings(SearchText.Trim(), SelectedStatus);
                Listings.Clear();
                foreach (var l in list)
                    Listings.Add(l);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load listings: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        public void ApproveListing(Guid listingId)
        {
            (bool ok, string msg) = _service.ApproveListing(listingId);
            MessageBox.Show(msg,
                ok ? "Thành công" : "Lỗi",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);
            if (ok) LoadListings();
        }

        public void RejectListing(Guid listingId)
        {
            (bool ok, string msg) = _service.RejectListing(listingId);
            MessageBox.Show(msg,
                ok ? "Thành công" : "Lỗi",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);
            if (ok) LoadListings();
        }

        public void RemoveListing(Guid listingId)
        {
            (bool ok, string msg) = _service.RemoveListing(listingId);
            MessageBox.Show(msg,
                ok ? "Thành công" : "Lỗi",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);
            if (ok) LoadListings();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
