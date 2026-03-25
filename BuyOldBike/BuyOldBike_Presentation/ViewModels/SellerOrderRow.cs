using System;

namespace BuyOldBike_Presentation.ViewModels
{
    public class SellerOrderRow
    {
        public Guid OrderId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BicycleTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string DepositStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
    }
}