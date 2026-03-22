using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BuyOldBike_BLL.Services.Feedback;
using BuyOldBike_BLL.Features.Feedback;

namespace BuyOldBike_Presentation.Controls
{
    /// <summary>
    /// Interaction logic for SellerReviewsControl.xaml
    /// </summary>
    public partial class SellerReviewsControl : UserControl
    {
        private ReviewService? _reviewService;
        private Guid _sellerId;

        public SellerReviewsControl()
        {
            InitializeComponent();
            _reviewService = new ReviewService();
        }

        /// <summary>
        /// Load reviews for a seller
        /// </summary>
        public void LoadSellerReviews(Guid sellerId)
        {
            _sellerId = sellerId;
            
            try
            {
                var (reviews, avgRating, totalReviews, totalPages) = _reviewService.GetSellerReviewsPaginated(sellerId, 1, 50);
                var (_, _, distribution) = _reviewService.GetSellerRatingStats(sellerId);

                // Update header
                AverageRatingText.Text = avgRating.ToString("F1");
                TotalReviewsText.Text = $"{totalReviews} đánh giá";

                // Update rating distribution bars
                if (totalReviews > 0)
                {
                    // Calculate percentage widths and set max width to 150 pixels
                    const double maxBarWidth = 150;
                    Rating5Bar.Width = (distribution[5] * maxBarWidth / totalReviews);
                    Rating4Bar.Width = (distribution[4] * maxBarWidth / totalReviews);
                    Rating3Bar.Width = (distribution[3] * maxBarWidth / totalReviews);
                    Rating2Bar.Width = (distribution[2] * maxBarWidth / totalReviews);
                    Rating1Bar.Width = (distribution[1] * maxBarWidth / totalReviews);

                    Rating5Count.Text = distribution[5].ToString();
                    Rating4Count.Text = distribution[4].ToString();
                    Rating3Count.Text = distribution[3].ToString();
                    Rating2Count.Text = distribution[2].ToString();
                    Rating1Count.Text = distribution[1].ToString();
                }

                // Load reviews
                var reviewDtos = new ObservableCollection<ReviewDto>();
                foreach (var review in reviews)
                {
                    reviewDtos.Add(new ReviewDto
                    {
                        ReviewId = review.ReviewId,
                        OrderId = review.OrderId ?? Guid.Empty,
                        BuyerName = review.Buyer?.Email ?? "Anonymous",
                        BuyerEmail = review.Buyer?.Email,
                        Rating = review.Rating ?? 0,
                        Description = review.Description,
                        CreatedAt = review.CreatedAt ?? DateTime.UtcNow
                    });
                }

                ReviewsListBox.ItemsSource = reviewDtos;
                EmptyStatePanel.Visibility = reviewDtos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải đánh giá: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
