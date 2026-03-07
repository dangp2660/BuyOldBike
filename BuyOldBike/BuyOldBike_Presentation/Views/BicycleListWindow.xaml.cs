using System.Windows;

namespace BuyOldBike_Presentation.Views
{
    public partial class BicycleListWindow : Window
    {
        public BicycleListWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
