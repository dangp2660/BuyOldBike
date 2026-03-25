using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.Views;
using System.Windows;

namespace BuyOldBike_Presentation.State
{
    public static class RoleNavigator
    {
        public static void NavigateToHome(Window fromWindow)
        {
            var user = AppSession.CurrentUser;
            if (user == null)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                fromWindow.Close();
                return;
            }

            var home = CreateHomeWindow(user);
            home.Show();
            fromWindow.Close();
        }

        public static bool EnsureRole(Window currentWindow, params string[] allowedRoles)
        {
            var user = AppSession.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Vui lòng đăng nhập để tiếp tục.", "Không có quyền truy cập", MessageBoxButton.OK, MessageBoxImage.Warning);
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                currentWindow.Close();
                return false;
            }

            if (allowedRoles.Length == 0) return true;

            var currentRole = Normalize(user.Role);
            foreach (var role in allowedRoles)
            {
                if (currentRole == Normalize(role)) return true;
            }

            MessageBox.Show("Tài khoản của bạn không có quyền truy cập khu vực này.", "Không có quyền truy cập", MessageBoxButton.OK, MessageBoxImage.Warning);
            var home = CreateHomeWindow(user);
            home.Show();
            currentWindow.Close();
            return false;
        }

        public static Window CreateHomeWindow(User user)
        {
            var role = Normalize(user.Role);
            if (role == Normalize(RoleConstants.Seller)) return new SellerWindow();
            if (role == Normalize(RoleConstants.Inspector)) return new InspectorWindow();
            if (role == Normalize(RoleConstants.Admin)) return new AdminWindow();
            return new BicycleListWindow();
        }

        private static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();
    }
}
