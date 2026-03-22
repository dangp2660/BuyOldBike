using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Feedback;
using BuyOldBike_BLL.Features.Feedback;
using System;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Services.Feedback
{
    public class ReviewService
    {
        private readonly ReviewRepository _reviewRepository;
        private readonly ReviewValidator _validator;

        public ReviewService()
        {
            _reviewRepository = new ReviewRepository();
            _validator = new ReviewValidator();
        }

        public ReviewService(ReviewRepository reviewRepository, ReviewValidator validator)
        {
            _reviewRepository = reviewRepository;
            _validator = validator;
        }

        /// <summary>
        /// Submit a new review
        /// </summary>
        public (bool success, string message, Review? review) SubmitReview(
            Guid orderId, Guid buyerId, Guid sellerId, int rating, string description)
        {
            // Clear validator
            _validator.Clear();

            // Validate inputs
            _validator
                .ValidateOrderId(orderId)
                .ValidateUserIds(buyerId, sellerId)
                .ValidateRating(rating)
                .ValidateDescription(description);

            if (!_validator.IsValid())
            {
                return (false, _validator.GetErrorMessage(), null);
            }

            // Check if review already exists
            if (_reviewRepository.ReviewExistsForOrder(orderId))
            {
                return (false, "A review already exists for this order", null);
            }

            try
            {
                var review = _reviewRepository.CreateReview(orderId, buyerId, sellerId, rating, description);
                return (true, "Review submitted successfully", review);
            }
            catch (Exception ex)
            {
                return (false, $"Error submitting review: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Update an existing review
        /// </summary>
        public (bool success, string message) UpdateReview(Guid reviewId, int rating, string description)
        {
            _validator.Clear();

            _validator
                .ValidateRating(rating)
                .ValidateDescription(description);

            if (!_validator.IsValid())
            {
                return (false, _validator.GetErrorMessage());
            }

            try
            {
                _reviewRepository.UpdateReview(reviewId, rating, description);
                return (true, "Review updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating review: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a review
        /// </summary>
        public bool DeleteReview(Guid reviewId)
        {
            try
            {
                _reviewRepository.DeleteReview(reviewId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get all reviews for a seller with rating
        /// </summary>
        public (List<Review> reviews, double averageRating, int totalReviews) GetSellerReviews(Guid sellerId)
        {
            var reviews = _reviewRepository.GetReviewsBySellerId(sellerId);
            var avgRating = _reviewRepository.GetAverageSellerRating(sellerId);
            return (reviews, avgRating, reviews.Count);
        }

        /// <summary>
        /// Get paginated reviews for a seller
        /// </summary>
        public (List<Review> reviews, double averageRating, int totalReviews, int totalPages) GetSellerReviewsPaginated(
            Guid sellerId, int pageNumber = 1, int pageSize = 5)
        {
            var (reviews, total) = _reviewRepository.GetSellerReviewsPaginated(sellerId, pageNumber, pageSize);
            var avgRating = _reviewRepository.GetAverageSellerRating(sellerId);
            int totalPages = (int)Math.Ceiling(total / (double)pageSize);

            return (reviews, avgRating, total, totalPages);
        }

        /// <summary>
        /// Get seller rating statistics
        /// </summary>
        public (double average, int total, Dictionary<int, int> distribution) GetSellerRatingStats(Guid sellerId)
        {
            return _reviewRepository.GetSellerRatingStats(sellerId);
        }

        /// <summary>
        /// Get review for a specific order
        /// </summary>
        public Review? GetReviewByOrderId(Guid orderId)
        {
            return _reviewRepository.GetReviewByOrderId(orderId);
        }

        /// <summary>
        /// Get seller's average rating (0-5)
        /// </summary>
        public double GetSellerAverageRating(Guid sellerId)
        {
            return _reviewRepository.GetAverageSellerRating(sellerId);
        }

        /// <summary>
        /// Get all reviews submitted by a buyer
        /// </summary>
        public List<Review> GetReviewsByBuyer(Guid buyerId)
        {
            return _reviewRepository.GetReviewsByBuyerId(buyerId);
        }
    }
}
