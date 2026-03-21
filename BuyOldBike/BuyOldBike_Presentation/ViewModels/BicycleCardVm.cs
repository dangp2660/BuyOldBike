using System;

namespace BuyOldBike_Presentation.ViewModels
{
    public class BicycleCardVm
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
        public string MetaText { get; set; } = "";
        public string? FirstImageUrl { get; set; }
    }
}
