using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace BuyOldBike_Presentation.ViewModels
{
    public class ListingDetailViewModel
    {
        public Listing? ListingBike { get; private set; }
        public ObservableCollection<BitmapImage> Images { get; } = new ObservableCollection<BitmapImage>();

        public void Load(Guid listingId)
        {
            BikePostRepository _bikeRepo = new();
            ListingBike = _bikeRepo.GetListingDetailById(listingId);

            Images.Clear();
            if (ListingBike?.ListingImages == null) return;

            foreach (var img in ListingBike.ListingImages)
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
