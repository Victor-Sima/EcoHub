using EcoHub.Admin.Services;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class UsersView : UserControl
    {
        public UsersView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                var api = new ApiService();
                var users = await api.GetUsersAsync();
                UsersGrid.ItemsSource = users;
            };
        }
    }
}
