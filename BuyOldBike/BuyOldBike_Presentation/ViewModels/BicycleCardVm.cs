using System;

namespace BuyOldBike_Presentation.ViewModels
{
    public class BicycleCardVm
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
        public string BrandName { get; set; } = "";
        public string BikeTypeName { get; set; } = "";
        public string MetaText { get; set; } = "";
        public string? FirstImageUrl { get; set; }

        public double SellerRating { get; set; }
        public int SellerTotalReviews { get; set; }

        public string SellerRatingText =>
            SellerTotalReviews <= 0 ? "Chưa có đánh giá" : $"{SellerRating:0.00}/5 ({SellerTotalReviews})";
    }
}
