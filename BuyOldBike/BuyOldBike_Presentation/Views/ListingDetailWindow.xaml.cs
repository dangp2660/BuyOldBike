using BuyOldBike_Presentation.ViewModels;
using System;
using System.Windows;

namespace BuyOldBike_Presentation.Views
{
    public partial class ListingDetailWindow : Window
    {
        public ListingDetailWindow(Guid listingId)
        {
            InitializeComponent();
            var vm = new ListingDetailViewModel();
            vm.Load(listingId);
            if (vm.ListingBike == null)
            {
                MessageBox.Show("Không tìm thấy listing.");
                Close();
                return;
            }
            DataContext = vm;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
