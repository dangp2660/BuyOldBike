using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using System;
using System.Collections.Generic;
using BuyOldBike_DAL.Constants;

namespace BuyOldBike_BLL.Services.Seller
{
    public class ListingService
    {
        private readonly BikePostRepository _bikePostRepo;

        public ListingService()
        {
            _bikePostRepo = new BikePostRepository();
        }

        public void CreateNewPost(Listing listing, List<string> imagePaths)
        {
            listing.ListingId = Guid.NewGuid();
            listing.Status = StatusConstants.ListingStatus.Pending_Inspection;
            listing.CreatedAt = DateTime.Now;

            var inspection = new Inspection
            {
                InspectionId = Guid.NewGuid(),
                Status = StatusConstants.InspectionStatus.Pending,
                CreatedAt = DateTime.Now,
                InspectionLocationId = Guid.Empty
            };

            _bikePostRepo.SaveFullListing(listing, imagePaths, inspection);
        }

        public List<Listing> GetListingsBySeller(Guid sellerId)
        {
            return _bikePostRepo.GetListingsBySellerId(sellerId);
        }

        // New: Update listing details (publish/edit)
        public void UpdateListing(Listing listing)
        {
            _bikePostRepo.UpdateListing(listing);
        }

        // New: Hide listing
        public void HideListing(Guid listingId)
        {
            _bikePostRepo.HideListing(listingId);
        }

        public void UnhideListing(Guid listingId)
        {
            _bikePostRepo.UnhideListing(listingId);
        }

        public void AddImagesToListing(Guid listingId, List<string> imageUrls)
        {
            _bikePostRepo.AddListingImages(listingId, imageUrls);
        }

        // New: Soft delete listing
        public void DeleteListing(Guid listingId)
        {
            _bikePostRepo.SoftDeleteListing(listingId);
        }
    }
}
