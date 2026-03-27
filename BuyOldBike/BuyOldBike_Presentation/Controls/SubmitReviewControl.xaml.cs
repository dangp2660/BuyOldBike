using System;
using System.Windows;
using System.Windows.Controls;
using BuyOldBike_BLL.Services.Feedback;
using BuyOldBike_BLL.Features.Feedback;

namespace BuyOldBike_Presentation.Controls
{
    public partial class SubmitReviewControl : UserControl
    {
        private readonly ReviewService _reviewService;
        private int _selectedRating = 0;
        private Guid _orderId = Guid.Empty;
        private Guid _buyerId = Guid.Empty;
        private Guid _sellerId = Guid.Empty;
        private string? _sellerName;

        public event EventHandler<EventArgs>? ReviewSubmitted;

        public SubmitReviewControl()
        {
            InitializeComponent();
            _reviewService = new ReviewService();
            DescriptionTextBox.TextChanged += DescriptionTextBox_TextChanged;
        }

        /// <summary>
        /// Initialize the review form for an order
        /// </summary>
        public void InitializeReview(Guid orderId, Guid buyerId, Guid sellerId, string sellerName = "Seller")
        {
            _orderId = orderId;
            _buyerId = buyerId;
            _sellerId = sellerId;
            _sellerName = sellerName;

            SellerNameText.Text = sellerName;
            SellerEmailText.Text = "Bán ở BuyOldBike";
            ResetForm();
        }

        private void RatingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Name.Replace("Star", "").Replace("Btn", ""), out int rating))
            {
                _selectedRating = rating;
                UpdateStarDisplay();
            }
        }

        private void UpdateStarDisplay()
        {
            var stars = new[] { Star1Btn, Star2Btn, Star3Btn, Star4Btn, Star5Btn };
            
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].Foreground = System.Windows.Media.Brushes.LightGray;
            }

            for (int i = 0; i < _selectedRating; i++)
            {
                stars[i].Foreground = System.Windows.Media.Brushes.Gold;
            }

            RatingDisplayText.Text = $"{_selectedRating} sao";
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int charCount = DescriptionTextBox.Text.Length;
            CharacterCountText.Text = $"{charCount} / 1000 ký tự";

            if (charCount > 1000)
            {
                DescriptionTextBox.Text = DescriptionTextBox.Text.Substring(0, 1000);
                DescriptionTextBox.CaretIndex = 1000;
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageText.Visibility = Visibility.Collapsed;

            if (_selectedRating == 0)
            {
                ShowError("Vui lòng chọn đánh giá sao");
                return;
            }

            var description = (DescriptionTextBox.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(description) && description.Length < 10)
            {
                ShowError("Nhận xét (nếu có) phải có ít nhất 10 ký tự");
                return;
            }

            try
            {
                SubmitButton.IsEnabled = false;
                SubmitButton.Content = "Đang gửi...";

                var (success, message, review) = _reviewService.SubmitReview(
                    _orderId,
                    _buyerId,
                    _sellerId,
                    _selectedRating,
                    description
                );

                if (success)
                {
                    MessageBox.Show("Cảm ơn bạn đã gửi đánh giá!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    ReviewSubmitted?.Invoke(this, EventArgs.Empty);
                    ResetForm();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "Gửi đánh giá";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                try
                {
                    window.DialogResult = false;
                }
                catch
                {
                }

                window.Close();
                return;
            }

            ResetForm();
        }

        private void ShowError(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessageText.Visibility = Visibility.Visible;
        }

        private void ResetForm()
        {
            _selectedRating = 0;
            DescriptionTextBox.Clear();
            UpdateStarDisplay();
            CharacterCountText.Text = "0 / 1000 ký tự";
            ErrorMessageText.Visibility = Visibility.Collapsed;
        }
    }
}
