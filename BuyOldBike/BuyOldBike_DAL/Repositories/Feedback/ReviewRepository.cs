using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Feedback
{
    public class ReviewRepository
    {
        private readonly BuyOldBikeContext _db;

        public ReviewRepository()
        {
            _db = new BuyOldBikeContext();
        }

        /// <summary>
        /// Get all reviews for a specific seller
        /// </summary>
        public List<Review> GetReviewsBySellerId(Guid sellerId)
        {
            return _db.Reviews
                .Where(r => r.SellerId == sellerId)
                .Include(r => r.Buyer)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get reviews for a specific order
        /// </summary>
        public Review? GetReviewByOrderId(Guid orderId)
        {
            return _db.Reviews
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                .FirstOrDefault(r => r.OrderId == orderId);
        }

        /// <summary>
        /// Check if a review already exists for an order
        /// </summary>
        public bool ReviewExistsForOrder(Guid orderId)
        {
            return _db.Reviews.Any(r => r.OrderId == orderId);
        }

        /// <summary>
        /// Get average seller rating
        /// </summary>
        public double GetAverageSellerRating(Guid sellerId)
        {
            var reviews = _db.Reviews
                .Where(r => r.SellerId == sellerId && r.Rating.HasValue)
                .Select(r => r.Rating!.Value)
                .ToList();

            if (reviews.Count == 0) return 0;
            return Math.Round(reviews.Average(), 2);
        }

        /// <summary>
        /// Get seller rating statistics
        /// </summary>
        public (double average, int total, Dictionary<int, int> distribution) GetSellerRatingStats(Guid sellerId)
        {
            var reviews = _db.Reviews
                .Where(r => r.SellerId == sellerId && r.Rating.HasValue)
                .Select(r => r.Rating!.Value)
                .ToList();

            if (reviews.Count == 0)
                return (0, 0, new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } });

            var distribution = new Dictionary<int, int>
            {
                { 5, reviews.Count(r => r == 5) },
                { 4, reviews.Count(r => r == 4) },
                { 3, reviews.Count(r => r == 3) },
                { 2, reviews.Count(r => r == 2) },
                { 1, reviews.Count(r => r == 1) }
            };

            return (Math.Round(reviews.Average(), 2), reviews.Count, distribution);
        }

        /// <summary>
        /// Create a new review
        /// </summary>
        public Review CreateReview(Guid orderId, Guid buyerId, Guid sellerId, int rating, string description)
        {
            var review = new Review
            {
                ReviewId = Guid.NewGuid(),
                OrderId = orderId,
                BuyerId = buyerId,
                SellerId = sellerId,
                Rating = rating,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            _db.SaveChanges();
            return review;
        }

        /// <summary>
        /// Update an existing review
        /// </summary>
        public void UpdateReview(Guid reviewId, int rating, string description)
        {
            var review = _db.Reviews.Find(reviewId);
            if (review == null) return;

            review.Rating = rating;
            review.Description = description;
            _db.SaveChanges();
        }

        /// <summary>
        /// Delete a review
        /// </summary>
        public void DeleteReview(Guid reviewId)
        {
            var review = _db.Reviews.Find(reviewId);
            if (review != null)
            {
                _db.Reviews.Remove(review);
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Get reviews by buyer
        /// </summary>
        public List<Review> GetReviewsByBuyerId(Guid buyerId)
        {
            return _db.Reviews
                .Where(r => r.BuyerId == buyerId)
                .Include(r => r.Seller)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get paginated reviews for a seller
        /// </summary>
        public (List<Review> reviews, int total) GetSellerReviewsPaginated(Guid sellerId, int pageNumber, int pageSize)
        {
            var query = _db.Reviews
                .Where(r => r.SellerId == sellerId)
                .Include(r => r.Buyer)
                .OrderByDescending(r => r.CreatedAt);

            int total = query.Count();
            var reviews = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (reviews, total);
        }
    }
}
