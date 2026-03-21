using System.Windows;
using BuyOldBike_DAL.Constants;
using BuyOldBike_Presentation.State;

namespace BuyOldBike_Presentation.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            if (!RoleNavigator.EnsureRole(this, RoleConstants.Admin)) return;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutManager.Logout(this);
        }
    }
}
