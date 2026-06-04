using EcoHub.Admin.Services;
using EcoHub.Shared.Models;
using System.Windows;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadDashboardAsync();
        }

        public async Task LoadDashboardAsync()
        {
            var api = new ApiService();
            var stats = await api.GetDashboardStatsAsync(AppState.LastLoginAt);
            if (stats != null)
            {
                TotalUsersText.Text = stats.TotalUsers.ToString();
                NewUsersText.Text = stats.NewUsers.ToString();
                TotalOrdersText.Text = stats.TotalOrders.ToString();
                NewOrdersText.Text = stats.NewOrders.ToString();
                RevenueText.Text = stats.TotalRevenue.ToString("N2") + " MDL";
                LowStockGrid.ItemsSource = stats.LowStockProducts;
                TopProductsGrid.ItemsSource = stats.TopProducts;
            }
        }
    }
}
