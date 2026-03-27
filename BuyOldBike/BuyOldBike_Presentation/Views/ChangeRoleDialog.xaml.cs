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
    /// Interaction logic for ChangeRoleDialog.xaml
    /// </summary>
    public partial class ChangeRoleDialog : Window
    {
        public string SelectedRole { get; private set; } = string.Empty;

        public ChangeRoleDialog(User user)
        {
            InitializeComponent();
            TxtCurrentRole.Text = $"Role hiện tại: {user.Role}";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CmbNewRole.SelectedItem is ComboBoxItem item)
                SelectedRole = item.Tag?.ToString() ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
