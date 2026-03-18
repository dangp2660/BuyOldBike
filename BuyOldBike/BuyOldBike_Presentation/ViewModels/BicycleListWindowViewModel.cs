using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;

namespace BuyOldBike_Presentation.ViewModels
{
    public class BicycleListWindowViewModel
    {
        public ObservableCollection<BicycleCardVm> Listings { get; } = new ObservableCollection<BicycleCardVm>();

        public void Load()
        {
            using var db = new BuyOldBikeContext();

            List<Listing> items = db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Include(l => l.ListingImages)
                .Where(l => l.Status == StatusConstants.ListingStatus.Available)
                .OrderByDescending(l => l.CreatedAt)
                .AsNoTracking()
                .ToList();

            Listings.Clear();
            foreach (Listing l in items)
            {
                string brand = l.Brand?.BrandName ?? "";
                string type = l.BikeType?.Name ?? "";
                string meta = string.IsNullOrWhiteSpace(type) ? brand : $"{brand} • {type}";
                string? firstImageUrl = l.ListingImages?.FirstOrDefault()?.ImageUrl;

                Listings.Add(new BicycleCardVm
                {
                    ListingId = l.ListingId,
                    Title = l.Title ?? "",
                    Price = l.Price ?? 0,
                    MetaText = meta,
                    FirstImageUrl = firstImageUrl
                });
            }
        }
    }
}
