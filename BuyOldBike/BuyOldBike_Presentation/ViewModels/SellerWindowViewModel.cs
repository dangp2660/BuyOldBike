using BuyOldBike_BLL.Services.Seller;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.ViewModels
{
    public class SellerWindowViewModel
    {
        private readonly ListingService _listingService;
        private readonly OrderService _orderService;

        public ObservableCollection<BitmapImage> SelectedImagePreviews { get; } = new ObservableCollection<BitmapImage>();
        public ObservableCollection<SellerListingRow> SellerListings { get; } = new ObservableCollection<SellerListingRow>();
        public ObservableCollection<SellerOrderRow> SellerOrders { get; } = new ObservableCollection<SellerOrderRow>();

        public SellerWindowViewModel()
        {
            _listingService = new ListingService();
            _orderService = new OrderService();
        }

        public void CreateNewPost(Listing listing, List<string> imagePaths)
        {
            _listingService.CreateNewPost(listing, imagePaths);
        }

        public void LoadSellerListings(Guid sellerId)
        {
            SellerListings.Clear();

            List<Listing> listings = _listingService.GetListingsBySeller(sellerId);
            foreach (Listing listing in listings)
            {
                SellerListings.Add(new SellerListingRow
                {
                    ListingId = listing.ListingId,
                    Title = listing.Title ?? string.Empty,
                    Price = listing.Price ?? 0,
                    Status = listing.Status ?? string.Empty,
                    Views = 0,
                    IsPending = string.Equals(listing.Status, StatusConstants.ListingStatus.Pending_Inspection, StringComparison.Ordinal)
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
            foreach (var o in orders)
            {
                SellerOrders.Add(new SellerOrderRow
                {
                    OrderId = o.OrderId,
                    BuyerName = o.Buyer?.Email ?? string.Empty,
                    BicycleTitle = o.Listing?.Title ?? string.Empty,
                    Price = o.TotalAmount ?? 0,
                    DepositStatus = "N/A",
                    OrderStatus = o.Status ?? string.Empty
                });
            }
        }

        public void UpdateOrderStatus(Guid orderId, string newStatus)
        {
            _orderService.UpdateOrderStatus(orderId, newStatus);
        }
    }
}
