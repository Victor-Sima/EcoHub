using EcoHub.Admin.Services;
using EcoHub.Admin.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace EcoHub.Admin
{
    public partial class MainWindow : Window
    {
        private readonly SignalRService _signalR = new();
        private int _notificationCount = 0;
        private DashboardView? _dashboardView;

        public MainWindow()
        {
            InitializeComponent();
            _signalR.OnNotificationReceived += OnNotificationReceived;
            _signalR.OnConnected += () => UpdateConnectionStatus(true);
            _signalR.OnDisconnected += () => UpdateConnectionStatus(false);
        }

        private async void LoginView_LoginSuccess(object? sender, EventArgs e)
        {
            LoginView.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;

            _dashboardView = new DashboardView();
            ContentFrame.Navigate(_dashboardView);

            await _signalR.StartAsync();
            await ShowStartupSummaryAsync();
        }

        private async Task ShowStartupSummaryAsync()
        {
            var api = new ApiService();
            var since = AppState.LastLoginAt ?? DateTime.MinValue;
            var newUsers = await api.GetNewUsersAsync(since);
            var newOrders = await api.GetNewOrdersAsync(since);

            if ((newUsers?.Count > 0 || newOrders?.Count > 0) && AppState.LastLoginAt != null)
            {
                var message = $"Since your last login ({AppState.LastLoginAt:yyyy-MM-dd HH:mm}):\n\n";
                message += $"New users: {newUsers?.Count ?? 0}\n";
                message += $"New orders: {newOrders?.Count ?? 0}\n";
                MessageBox.Show(message, "Activity Summary", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            AppState.LastLoginAt = DateTime.UtcNow;
        }

        private void OnNotificationReceived(EcoHub.Shared.Models.NotificationDto notification)
        {
            Dispatcher.Invoke(() =>
            {
                _notificationCount++;
                NotificationBadgeText.Text = _notificationCount.ToString();
                NotificationBadge.Visibility = Visibility.Visible;
        
                // Also refresh dashboard if visible
                _dashboardView?.LoadDashboardAsync();
            });
        }
        
        private void SetActiveNav(Button? active)
        {
            foreach (var btn in new[] { NavDashboardBtn, NavUsersBtn, NavProductsBtn, NavCategoriesBtn, NavOrdersBtn, NavSettingsBtn })
            {
                btn.Tag = btn == active ? "Active" : null;
            }
        }
        
        private void UpdateConnectionStatus(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionIndicator.Fill = connected ? Brushes.LimeGreen : new SolidColorBrush(Color.FromRgb(0xBD, 0xBD, 0xBD));
                ConnectionText.Text = connected ? "Connected" : "Disconnected";
            });
        }
        
        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavDashboardBtn);
            _dashboardView = new DashboardView();
            ContentFrame.Navigate(_dashboardView);
        }
        
        private void NavUsers_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavUsersBtn);
            ContentFrame.Navigate(new UsersView());
        }
        
        private void NavProducts_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavProductsBtn);
            ContentFrame.Navigate(new ProductsView());
        }
        
        private void NavCategories_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavCategoriesBtn);
            ContentFrame.Navigate(new CategoriesView());
        }
        
        private void NavOrders_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavOrdersBtn);
            ContentFrame.Navigate(new OrdersView());
        }
        
        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavSettingsBtn);
            ContentFrame.Navigate(new SettingsView());
        }
        
        private void Notifications_Click(object sender, RoutedEventArgs e)
        {
            _notificationCount = 0;
            NotificationBadge.Visibility = Visibility.Collapsed;
            SetActiveNav(null);
            ContentFrame.Navigate(new NotificationsView());
        }

        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            await _signalR.StopAsync();
            AppState.AuthToken = null;
            AppState.CurrentUser = null;
            MainContent.Visibility = Visibility.Collapsed;
            LoginView.Visibility = Visibility.Visible;
        }
    }
}
