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
                if (_reviewService == null) _reviewService = new ReviewService();

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
                    distribution.TryGetValue(5, out var c5);
                    distribution.TryGetValue(4, out var c4);
                    distribution.TryGetValue(3, out var c3);
                    distribution.TryGetValue(2, out var c2);
                    distribution.TryGetValue(1, out var c1);

                    Rating5Bar.Width = (c5 * maxBarWidth / totalReviews);
                    Rating4Bar.Width = (c4 * maxBarWidth / totalReviews);
                    Rating3Bar.Width = (c3 * maxBarWidth / totalReviews);
                    Rating2Bar.Width = (c2 * maxBarWidth / totalReviews);
                    Rating1Bar.Width = (c1 * maxBarWidth / totalReviews);

                    Rating5Count.Text = c5.ToString();
                    Rating4Count.Text = c4.ToString();
                    Rating3Count.Text = c3.ToString();
                    Rating2Count.Text = c2.ToString();
                    Rating1Count.Text = c1.ToString();
                }
                else
                {
                    Rating5Bar.Width = 0;
                    Rating4Bar.Width = 0;
                    Rating3Bar.Width = 0;
                    Rating2Bar.Width = 0;
                    Rating1Bar.Width = 0;

                    Rating5Count.Text = "0";
                    Rating4Count.Text = "0";
                    Rating3Count.Text = "0";
                    Rating2Count.Text = "0";
                    Rating1Count.Text = "0";
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
