using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Repositories.Seller
{
    public class ListingRepository
    {
        public class ListingModerationRepository
        {
            private readonly BuyOldBikeContext _db;

            public ListingModerationRepository()
            {
                _db = new BuyOldBikeContext();
            }

            public List<Listing> GetFilteredListings(string? searchTerm, string? status)
            {
                var query = _db.Listings
                               .Include(l => l.Seller)
                               .Include(l => l.Brand)
                               .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lower = searchTerm.ToLower();
                    query = query.Where(l =>
                        (l.Title != null && l.Title.ToLower().Contains(lower)) ||
                        (l.Seller != null && l.Seller.Email.ToLower().Contains(lower)));
                }

                if (!string.IsNullOrWhiteSpace(status) && status != "All status")
                    query = query.Where(l => l.Status == status);

                return query.OrderByDescending(l => l.CreatedAt).ToList();
            }

            public Listing? GetById(Guid listingId)
            {
                return _db.Listings
                          .Include(l => l.Seller)
                          .Include(l => l.Brand)
                          .FirstOrDefault(l => l.ListingId == listingId);
            }

            public bool UpdateStatus(Guid listingId, string newStatus)
            {
                var listing = _db.Listings.FirstOrDefault(l => l.ListingId == listingId);
                if (listing == null) return false;
                listing.Status = newStatus;
                return _db.SaveChanges() > 0;
            }
        }
    }
}
