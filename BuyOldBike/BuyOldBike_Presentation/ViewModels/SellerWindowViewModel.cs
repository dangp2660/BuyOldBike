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

        public ObservableCollection<BitmapImage> SelectedImagePreviews { get; } = new ObservableCollection<BitmapImage>();
        public ObservableCollection<SellerListingRow> SellerListings { get; } = new ObservableCollection<SellerListingRow>();

        public SellerWindowViewModel()
        {
            _listingService = new ListingService();
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
    }
}
