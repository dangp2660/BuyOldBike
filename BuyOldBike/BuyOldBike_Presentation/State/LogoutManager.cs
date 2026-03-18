using BuyOldBike_Presentation.Views;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace BuyOldBike_Presentation.State
{
    public static class LogoutManager
    {
        public static void Logout(Window? owner = null, bool confirm = true)
        {
            if (confirm && !ConfirmLogout(owner)) return;

            RunOnUiThread(() =>
            {
                AppSession.Clear();

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                loginWindow.Activate();

                var windowsToClose = Application.Current.Windows.Cast<Window>()
                    .Where(w => !ReferenceEquals(w, loginWindow))
                    .ToList();

                foreach (var window in windowsToClose)
                {
                    window.Close();
                }
            });
        }

        private static bool ConfirmLogout(Window? owner)
        {
            const string message = "Bạn chắc chắn muốn đăng xuất?";
            const string title = "Xác nhận đăng xuất";

            if (owner != null)
            {
                return MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            }

            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.Invoke(action);
        }
    }
}
