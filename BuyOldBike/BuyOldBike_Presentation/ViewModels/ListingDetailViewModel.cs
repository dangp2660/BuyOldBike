using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.ViewModels
{
    public class ListingDetailViewModel
    {
        public Listing? Listing { get; private set; }
        public ObservableCollection<BitmapImage> Images { get; } = new ObservableCollection<BitmapImage>();

        public void Load(Guid listingId)
        {
            BuyOldBikeContext db = new BuyOldBikeContext();
            Listing = db.Listings
                .Include(l => l.Seller)
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Include(l => l.ListingImages)
                .AsNoTracking()
                .FirstOrDefault(l => l.ListingId == listingId);

            Images.Clear();
            if (Listing?.ListingImages == null) return;

            foreach (var img in Listing.ListingImages)
            {
                var bitmap = TryCreateBitmap(img.ImageUrl);
                if (bitmap != null) Images.Add(bitmap);
            }
        }

        private static BitmapImage? TryCreateBitmap(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;

            try
            {
                string path = imageUrl;
                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
                {
                    path = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        imageUrl.Replace('/', Path.DirectorySeparatorChar)
                    );
                    uri = new Uri(path, UriKind.Absolute);
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
