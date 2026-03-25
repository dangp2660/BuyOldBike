using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BuyOldBike_Presentation.Views
{
    /// <summary>
    /// Interaction logic for OrderDetailDialog.xaml
    /// </summary>
    public partial class OrderDetailDialog : Window
    {
        public OrderDetailDialog(Order order)
        {
            InitializeComponent();
            TxtOrderId.Text = order.OrderId.ToString();
            TxtBuyer.Text = order.Buyer?.Email ?? "N/A";
            TxtListing.Text = order.Listing?.Title ?? "N/A";
            TxtSeller.Text = order.Listing?.Seller?.Email ?? "N/A";
            TxtAmount.Text = order.TotalAmount.HasValue
                ? $"{order.TotalAmount:N0} ₫"
                : "N/A";
            TxtStatus.Text = order.Status ?? "N/A";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
