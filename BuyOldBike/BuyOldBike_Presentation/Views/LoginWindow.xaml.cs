using BuyOldBike_BLL.Services.Auth;
using BuyOldBike_Presentation.State;
using System.Windows;
using static BuyOldBike_BLL.Services.Auth.LoginService;

namespace BuyOldBike_Presentation.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text;
            string password = txtPassword.Password;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both email and password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!email.Contains(".") || !email.Contains("@"))
            {
                MessageBox.Show("Please enter a valid email address.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoginService loginService = new LoginService();
            var result = loginService.LoginWithResult(email, password);
            switch (result)
            {
                case LoginResult.Success:
                    var user = loginService.LoginAndGetUser(email, password);
                    AppSession.SetCurrentUser(user!);
                    RoleNavigator.NavigateToHome(this);
                    break;

                case LoginResult.Suspended:
                    MessageBox.Show(
                        "Your account has been locked.\nPlease contact the administrator for assistance.",
                        "Account locked",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;

                case LoginResult.InvalidCredentials:
                    MessageBox.Show(
                        "Invalid email or password. " +
                        "Please try again.", "Login Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            Close();
        }
    }
}
