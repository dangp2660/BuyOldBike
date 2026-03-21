using System;

namespace BuyOldBike_Presentation.ViewModels
{
    public class SellerListingRow
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Views { get; set; }
        public bool IsPending { get; set; }
    }
}
