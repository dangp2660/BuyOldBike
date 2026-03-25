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
    /// Interaction logic for UserProfileDialog.xaml
    /// </summary>
    public partial class UserProfileDialog : Window
    {
        public UserProfileDialog(User user)
        {
            InitializeComponent();
            TxtUserId.Text = user.UserId.ToString();
            TxtEmail.Text = user.Email;
            TxtPhone.Text = user.PhoneNumber;
            TxtRole.Text = user.Role;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
