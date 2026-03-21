﻿﻿using System;
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
using BuyOldBike_Presentation.ViewModels;

namespace BuyOldBike_Presentation.Views
{
    /// <summary>
    /// Interaction logic for BicycleListWindow.xaml
    /// </summary>
    public partial class BicycleListWindow : Window
    {
        private readonly BicycleListWindowViewModel _vm = new BicycleListWindowViewModel();

        public BicycleListWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += BicycleListWindow_Loaded;
            Activated += BicycleListWindow_Activated;
            Unloaded += BicycleListWindow_Unloaded;
            AppSession.CurrentUserChanged += AppSession_CurrentUserChanged;
        }

        private void BicycleListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAuthUi();
            TryLoadListings();
        }

        private void BicycleListWindow_Activated(object? sender, EventArgs e)
        {
            TryLoadListings();
        }

        private void BicycleListWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            AppSession.CurrentUserChanged -= AppSession_CurrentUserChanged;
            Activated -= BicycleListWindow_Activated;
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
            LogoutManager.Logout(this);
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

        private void TryLoadListings()
        {
            try
            {
                _vm.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách xe: {ex.Message}");
            }
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

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            _vm.PrevPage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _vm.NextPage();
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not int page) return;
            _vm.GoToPage(page);
        }

        private void ListingCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            if (fe.DataContext is not BicycleCardVm card) return;
            if (card.ListingId == Guid.Empty) return;

            var detailWindow = new ListingDetailWindow(card.ListingId);
            detailWindow.Owner = this;
            detailWindow.ShowDialog();
        }
    }
}
