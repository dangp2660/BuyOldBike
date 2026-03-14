﻿using System;
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
using BuyOldBike_Presentation.State;

namespace BuyOldBike_Presentation.Views
{
    /// <summary>
    /// Interaction logic for BicycleListWindow.xaml
    /// </summary>
    public partial class BicycleListWindow : Window
    {
        public BicycleListWindow()
        {
            InitializeComponent();
            Loaded += BicycleListWindow_Loaded;
            Unloaded += BicycleListWindow_Unloaded;
            AppSession.CurrentUserChanged += AppSession_CurrentUserChanged;
        }

        private void BicycleListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAuthUi();
        }

        private void BicycleListWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            AppSession.CurrentUserChanged -= AppSession_CurrentUserChanged;
        }

        private void AppSession_CurrentUserChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(UpdateAuthUi);
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void btnProfile_Click(object sender, RoutedEventArgs e)
        {
            if (btnProfile.ContextMenu == null) return;
            btnProfile.ContextMenu.PlacementTarget = btnProfile;
            btnProfile.ContextMenu.IsOpen = true;
        }

        private void MenuProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.IsAuthenticated) return;
            var profileWindow = new ProfileWindow();
            profileWindow.Owner = this;
            profileWindow.ShowDialog();
        }

        private void MenuLogout_Click(object sender, RoutedEventArgs e)
        {
            AppSession.Clear();
        }

        private void UpdateAuthUi()
        {
            if (AppSession.IsAuthenticated)
            {
                btnLogin.Visibility = Visibility.Collapsed;
                btnProfile.Visibility = Visibility.Visible;
                btnProfile.Content = GetProfileBadgeText();
                return;
            }

            btnProfile.Visibility = Visibility.Collapsed;
            btnLogin.Visibility = Visibility.Visible;
        }

        private string GetProfileBadgeText()
        {
            var user = AppSession.CurrentUser;
            if (user == null) return "👤";

            var source = user.Email ?? "";
            if (string.IsNullOrWhiteSpace(source)) return "👤";

            var part = source.Split('@').FirstOrDefault() ?? "";
            part = part.Trim();
            if (part.Length == 0) return "👤";

            return part.Substring(0, Math.Min(2, part.Length)).ToUpperInvariant();
        }
    }
}
