using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_BLL.Features.Chats;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.ViewModels
{
    public class SellerWindowViewModel : INotifyPropertyChanged
    {
        private readonly ListingService _listingService;
        private readonly OrderService _orderService;
        private readonly ChatService _chatService;

        public ObservableCollection<BitmapImage> SelectedImagePreviews { get; } = new ObservableCollection<BitmapImage>();
        public ObservableCollection<SellerListingRow> SellerListings { get; } = new ObservableCollection<SellerListingRow>();
        public ObservableCollection<SellerOrderRow> SellerOrders { get; } = new ObservableCollection<SellerOrderRow>();

        private int _totalListingsCount;
        public int TotalListingsCount
        {
            get => _totalListingsCount;
            private set
            {
                if (_totalListingsCount == value) return;
                _totalListingsCount = value;
                OnPropertyChanged();
            }
        }

        private int _activeListingsCount;
        public int ActiveListingsCount
        {
            get => _activeListingsCount;
            private set
            {
                if (_activeListingsCount == value) return;
                _activeListingsCount = value;
                OnPropertyChanged();
            }
        }

        private int _pendingOrdersCount;
        public int PendingOrdersCount
        {
            get => _pendingOrdersCount;
            private set
            {
                if (_pendingOrdersCount == value) return;
                _pendingOrdersCount = value;
                OnPropertyChanged();
            }
        }

        private int _unreadMessageCount;
        public int UnreadMessageCount
        {
            get => _unreadMessageCount;
            private set
            {
                if (_unreadMessageCount == value) return;
                _unreadMessageCount = value;
                OnPropertyChanged();
            }
        }

        public SellerWindowViewModel()
        {
            _listingService = new ListingService();
            _orderService = new OrderService();
            _chatService = new ChatService();
        }

        public void CreateNewPost(Listing listing, List<string> imagePaths)
        {
            _listingService.CreateNewPost(listing, imagePaths);
        }

        public void LoadSellerListings(Guid sellerId)
        {
            SellerListings.Clear();

            List<Listing> listings = _listingService.GetListingsBySeller(sellerId);
            TotalListingsCount = listings.Count;
            ActiveListingsCount = listings.Count(l =>
                string.Equals(l.Status, StatusConstants.ListingStatus.Available, StringComparison.Ordinal));

            foreach (Listing listing in listings)
            {
                SellerListings.Add(new SellerListingRow
                {
                    ListingId = listing.ListingId,
                    Title = listing.Title ?? string.Empty,
                    Price = listing.Price ?? 0,
                    Status = listing.Status ?? string.Empty,
                    Views = listing.Views
                });
            }
        }

        public void UpdateListing(Listing listing)
        {
            _listingService.UpdateListing(listing);
        }

        public void HideListing(Guid listingId)
        {
            _listingService.HideListing(listingId);
        }

        public void UnhideListing(Guid listingId)
        {
            _listingService.UnhideListing(listingId);
        }

        public void DeleteListing(Guid listingId)
        {
            _listingService.DeleteListing(listingId);
        }

        // Orders
        public void LoadSellerOrders(Guid sellerId)
        {
            SellerOrders.Clear();
            var orders = _orderService.GetOrdersBySellerId(sellerId);

            PendingOrdersCount = orders.Count(o =>
                string.Equals(o.Status, StatusConstants.OrdersStatus.Deposit_Pending, StringComparison.Ordinal) ||
                string.Equals(o.Status, StatusConstants.OrdersStatus.Deposit_Paid, StringComparison.Ordinal) ||
                string.Equals(o.Status, StatusConstants.OrdersStatus.Disputed, StringComparison.Ordinal));

            foreach (var o in orders)
            {
                var deliveryInfo = string.Empty;
                if (!string.IsNullOrWhiteSpace(o.DeliveryDetail) ||
                    !string.IsNullOrWhiteSpace(o.DeliveryWard) ||
                    !string.IsNullOrWhiteSpace(o.DeliveryDistrict) ||
                    !string.IsNullOrWhiteSpace(o.DeliveryProvince))
                {
                    var contact = string.Empty;
                    if (!string.IsNullOrWhiteSpace(o.DeliveryFullName) && !string.IsNullOrWhiteSpace(o.DeliveryPhoneNumber))
                        contact = $"{o.DeliveryFullName} - {o.DeliveryPhoneNumber}";
                    else if (!string.IsNullOrWhiteSpace(o.DeliveryFullName))
                        contact = o.DeliveryFullName;
                    else if (!string.IsNullOrWhiteSpace(o.DeliveryPhoneNumber))
                        contact = o.DeliveryPhoneNumber;

                    var addr = $"{o.DeliveryDetail}, {o.DeliveryWard}, {o.DeliveryDistrict}, {o.DeliveryProvince}";
                    deliveryInfo = string.IsNullOrWhiteSpace(contact) ? addr : $"{contact} | {addr}";
                }

                SellerOrders.Add(new SellerOrderRow
                {
                    OrderId = o.OrderId,
                    BuyerName = o.Buyer?.Email ?? string.Empty,
                    BicycleTitle = o.Listing?.Title ?? string.Empty,
                    Price = o.TotalAmount ?? 0,
                    DepositStatus = "N/A",
                    OrderStatus = o.Status ?? string.Empty,
                    DeliveryInfo = deliveryInfo
                });
            }
        }

        public void UpdateOrderStatus(Guid orderId, string newStatus)
        {
            _orderService.UpdateOrderStatus(orderId, newStatus);
        }

        public void LoadUnreadMessageCount(Guid sellerId)
        {
            UnreadMessageCount = _chatService.GetUnreadCount(sellerId);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
