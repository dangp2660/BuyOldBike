using System;
using System.ComponentModel;
using BuyOldBike_DAL.Constants;

namespace BuyOldBike_Presentation.ViewModels
{
    public class SellerListingRow : INotifyPropertyChanged
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Views { get; set; }
        public bool IsPending { get; set; }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsAvailable));
                OnPropertyChanged(nameof(IsHidden));
                OnPropertyChanged(nameof(CanToggle));
            }
        }

        public bool IsAvailable => string.Equals(Status, StatusConstants.ListingStatus.Available, StringComparison.Ordinal);
        public bool IsHidden => string.Equals(Status, StatusConstants.ListingStatus.Hidden, StringComparison.Ordinal);
        public bool IsDeleted => string.Equals(Status, StatusConstants.ListingStatus.Deleted, StringComparison.Ordinal);
        public bool IsNotDeleted => !IsDeleted;

        public bool CanToggle => IsAvailable || IsHidden;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
