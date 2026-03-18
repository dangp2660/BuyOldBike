using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Seller
{
    public class BikePostRepository
    {
        private readonly BuyOldBikeContext _db;

        public BikePostRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public void SaveFullListing(Listing listing, List<string> imageUrls, Inspection inspection)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                _db.Listings.Add(listing);
                _db.SaveChanges();

                foreach (var url in imageUrls)
                {
                    var img = new ListingImage
                    {
                        ImageId = Guid.NewGuid(),
                        ListingId = listing.ListingId,
                        ImageUrl = url
                    };
                    _db.ListingImages.Add(img);
                }

                inspection.ListingId = listing.ListingId;
                inspection.InspectionLocationId = EnsureInspectionLocationId(inspection.InspectionLocationId);
                if (inspection.CreatedAt == default)
                {
                    inspection.CreatedAt = DateTime.Now;
                }
                _db.Inspections.Add(inspection);

                _db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<Inspection> GetPendingInspections()
        {
            return _db.Inspections
                .Include(i => i.Listing)
                .ThenInclude(l => l.Seller)
                .Where(i => i.Status == StatusConstants.InspectionStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();
        }

        public void UpdateInspectionResult(Guid inspectionId, string result,
            string listingStatus, int overallScore, string? notes)
        {
            Inspection inspection = _db.Inspections.Find(inspectionId);
            if (inspection != null)
            {
                inspection.Status = StatusConstants.InspectionStatus.Completed;
                inspection.Result = result;
                inspection.OverallScore = overallScore;

                if (result == StatusConstants.InspectionResult.Passed)
                {
                    inspection.RejectReason = null;
                }
                else
                {
                    inspection.RejectReason = notes;
                }

                Listing listing = _db.Listings.Find(inspection.ListingId);
                if (listing != null)
                {
                    listing.Status = listingStatus;
                }

                _db.SaveChanges();
            }
        }

        public List<Listing> GetListingsBySellerId(Guid sellerId)
        {
            return _db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Where(l => l.SellerId == sellerId)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        private Guid EnsureInspectionLocationId(Guid inspectionLocationId)
        {
            if (inspectionLocationId != Guid.Empty)
            {
                var exists = _db.InspectionLocations.Any(x => x.InspectionLocationId == inspectionLocationId);
                if (exists) return inspectionLocationId;
            }

            var existing = _db.InspectionLocations
                .OrderBy(x => x.Type)
                .Select(x => x.InspectionLocationId)
                .FirstOrDefault();

            if (existing != Guid.Empty) return existing;

            var location = new InspectionLocation
            {
                InspectionLocationId = Guid.NewGuid(),
                Type = "Default",
                AddressLine = "Default",
                City = "Default"
            };
            _db.InspectionLocations.Add(location);
            _db.SaveChanges();
            return location.InspectionLocationId;
        }


        public List<Listing> GetAvailableListings()
        {
            return _db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Include(l => l.ListingImages)
                .Where(l => l.Status == StatusConstants.ListingStatus.Available)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }
    }
}
