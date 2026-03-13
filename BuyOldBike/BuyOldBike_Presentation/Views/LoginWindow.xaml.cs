using BuyOldBike_BLL.Services.Auth;
using System.Windows;

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
            bool isAuthenticated = loginService.Login(email, password);
            if (!isAuthenticated)
            {
                MessageBox.Show("Invalid email or password. Please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                BicycleListWindow bicycleListWindow = new BicycleListWindow();
                bicycleListWindow.Show();
                this.Close();
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
