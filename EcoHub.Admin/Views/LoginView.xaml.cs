using EcoHub.Admin.Services;
using System.Windows;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class LoginView : UserControl
    {
        public event EventHandler? LoginSuccess;

        public LoginView()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var api = new ApiService();
            var email = EmailTextBox.Text;
            var password = PasswordBox.Password;

            var result = await api.LoginAsync(email, password);
            if (result != null && result.User.Role == Shared.Enums.UserRole.Admin)
            {
                AppState.AuthToken = result.Token;
                AppState.CurrentUser = result.User;
                LoginSuccess?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorTextBlock.Text = "Invalid credentials or not an admin account.";
            }
        }
    }
}
