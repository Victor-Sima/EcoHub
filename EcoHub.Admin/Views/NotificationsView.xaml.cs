using EcoHub.Admin.Services;
using System.Windows;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadNotificationsAsync();
        }

        private async Task LoadNotificationsAsync()
        {
            var api = new ApiService();
            var notifications = await api.GetNotificationsAsync();
            NotificationsGrid.ItemsSource = notifications;
        }

        private async void MarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            var api = new ApiService();
            await api.MarkAllNotificationsReadAsync();
            await LoadNotificationsAsync();
        }
    }
}
