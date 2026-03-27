using System;
using System.Windows;

namespace BuyOldBike_Presentation.Views
{
    public partial class SubmitReviewWindow : Window
    {
        public SubmitReviewWindow(Guid orderId, Guid buyerId, Guid sellerId, string sellerName)
        {
            InitializeComponent();
            ReviewControl.InitializeReview(orderId, buyerId, sellerId, sellerName);
            ReviewControl.ReviewSubmitted += ReviewControl_ReviewSubmitted;
        }

        private void ReviewControl_ReviewSubmitted(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
