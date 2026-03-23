using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuyOldBike_DAL.Repositories.Seller
{
    public class BikePostRepository
    {
        public void SaveFullListing(Listing listing, List<string> imageUrls, Inspection inspection)
        {
            using var db = new BuyOldBikeContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                db.Listings.Add(listing);
                db.SaveChanges();

                foreach (var url in imageUrls)
                {
                    var img = new ListingImage
                    {
                        ImageId = Guid.NewGuid(),
                        ListingId = listing.ListingId,
                        ImageUrl = url
                    };
                    db.ListingImages.Add(img);
                }

                inspection.ListingId = listing.ListingId;
                inspection.InspectionLocationId = EnsureInspectionLocationId(inspection.InspectionLocationId);
                if (inspection.CreatedAt == default)
                {
                    inspection.CreatedAt = DateTime.Now;
                }
                db.Inspections.Add(inspection);

                db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void UpdateListing(Listing listing)
        {
            using var db = new BuyOldBikeContext();

            var existing = db.Listings.Find(listing.ListingId);
            if (existing == null) return;

            existing.Title = listing.Title;
            existing.Description = listing.Description;
            existing.Price = listing.Price;
            existing.BrandId = listing.BrandId;
            existing.BikeTypeId = listing.BikeTypeId;
            existing.FrameNumber = listing.FrameNumber;
            existing.UsageDuration = listing.UsageDuration;
            existing.Status = listing.Status ?? existing.Status;

            db.SaveChanges();
        }

        public void HideListing(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            var l = db.Listings.Find(listingId);
            if (l == null) return;
            l.Status = StatusConstants.ListingStatus.Hidden;

            db.SaveChanges();
        }

        public void UnhideListing(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            var l = db.Listings.Find(listingId);
            if (l == null) return;
            l.Status = StatusConstants.ListingStatus.Available;

            db.SaveChanges();
        }

        public void AddListingImages(Guid listingId, List<string> imageUrls)
        {
            using var db = new BuyOldBikeContext();

            var listing = db.Listings.Find(listingId);
            if (listing == null) return;

            foreach (var url in imageUrls)
            {
                db.ListingImages.Add(new ListingImage
                {
                    ImageId = Guid.NewGuid(),
                    ListingId = listingId,
                    ImageUrl = url
                });
            }

            db.SaveChanges();
        }

        public void SoftDeleteListing(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            var l = db.Listings.Find(listingId);
            if (l == null) return;
            l.Status = StatusConstants.ListingStatus.Deleted;

            db.SaveChanges();
        }

        public List<Inspection> GetPendingInspections()
        {
            using var db = new BuyOldBikeContext();

            return db.Inspections
                .Include(i => i.Listing)
                .ThenInclude(l => l.Seller)
                .Where(i => i.Status == StatusConstants.InspectionStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();
        }

        public void UpdateInspectionResult(Guid inspectionId, string result,
            string listingStatus, int overallScore, string? notes)
        {
            using var db = new BuyOldBikeContext();

            Inspection? inspection = db.Inspections.Find(inspectionId);
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

                Listing? listing = db.Listings.Find(inspection.ListingId);
                if (listing != null)
                {
                    listing.Status = listingStatus;
                }

                db.SaveChanges();
            }
        }

        public List<Listing> GetListingsBySellerId(Guid sellerId)
        {
            using var db = new BuyOldBikeContext();

            return db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Where(l => l.SellerId == sellerId)
                .Where(l => l.Status != StatusConstants.ListingStatus.Deleted)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        private Guid EnsureInspectionLocationId(Guid inspectionLocationId)
        {
            using var db = new BuyOldBikeContext();

            if (inspectionLocationId != Guid.Empty)
            {
                var exists = db.InspectionLocations.Any(x => x.InspectionLocationId == inspectionLocationId);
                if (exists) return inspectionLocationId;
            }

            var existing = db.InspectionLocations
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
            db.InspectionLocations.Add(location);
            db.SaveChanges();
            return location.InspectionLocationId;
        }

        public List<Listing> GetAvailableListings()
        {
            using var db = new BuyOldBikeContext();

            return db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Include(l => l.ListingImages)
                .Where(l => l.Status == StatusConstants.ListingStatus.Available)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        public Listing? GetListingDetailById(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            return db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Include(l => l.ListingImages)
                .FirstOrDefault(l => l.ListingId == listingId);
        }
    }
}
