using EcoHub.Admin.Services;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using System.Windows;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            var api = new ApiService();
            var orders = await api.GetOrdersAsync();
            OrdersGrid.ItemsSource = orders;
        }

        private async void UpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderDto order)
            {
                var panel = (sender as Button)?.Parent as StackPanel;
                var combo = panel?.FindName("StatusCombo") as ComboBox;
                if (combo?.SelectedItem is ComboBoxItem item && Enum.TryParse<OrderStatus>(item.Content.ToString(), out var status))
                {
                    var api = new ApiService();
                    await api.UpdateOrderStatusAsync(order.Id, status);
                    await LoadOrdersAsync();
                }
            }
        }
    }
}
