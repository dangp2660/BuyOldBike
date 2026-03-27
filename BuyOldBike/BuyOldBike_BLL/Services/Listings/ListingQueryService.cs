using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuyOldBike_BLL.Services.Listings
{
    public class ListingQueryService
    {
        public Listing? GetListingWithSeller(Guid listingId)
        {
            using var db = new BuyOldBikeContext();
            return db.Listings
                .AsNoTracking()
                .Include(l => l.Seller)
                .FirstOrDefault(l => l.ListingId == listingId);
        }

        public Order? GetOrderWithListingAndSeller(Guid orderId)
        {
            using var db = new BuyOldBikeContext();
            return db.Orders
                .AsNoTracking()
                .Include(o => o.Listing!)
                    .ThenInclude(l => l.Seller)
                .FirstOrDefault(o => o.OrderId == orderId);
        }
    }
}
