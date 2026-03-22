using System;

namespace BuyOldBike_BLL.Features.Feedback
{
    /// <summary>
    /// DTO for submitting a new review
    /// </summary>
    public class SubmitReviewDto
    {
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid SellerId { get; set; }
        public int Rating { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for reviewing information
    /// </summary>
    public class ReviewDto
    {
        public Guid ReviewId { get; set; }
        public Guid OrderId { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public int Rating { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for seller reputation
    /// </summary>
    public class SellerReputationDto
    {
        public Guid SellerId { get; set; }
        public string? SellerName { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }
}
