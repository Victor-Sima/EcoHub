using EcoHub.Admin.Services;
using EcoHub.Shared.Models;
using System.Windows;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class ProductsView : UserControl
    {
        public ProductsView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadProductsAsync();
        }

        private async Task LoadProductsAsync()
        {
            var api = new ApiService();
            var products = await api.GetProductsAsync();
            ProductsGrid.ItemsSource = products;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductDialog(new ProductDto { CategoryId = 1 });
            if (dialog.ShowDialog() == true)
            {
                _ = CreateProductAsync(dialog.Product);
            }
        }

        private async Task CreateProductAsync(ProductDto product)
        {
            var api = new ApiService();
            var request = new CreateProductRequest
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                DiscountPercentage = product.DiscountPercentage
            };
            await api.CreateProductAsync(request);
            await LoadProductsAsync();
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ProductDto product)
            {
                var dialog = new ProductDialog(product);
                if (dialog.ShowDialog() == true)
                {
                    _ = UpdateProductAsync(product.Id, dialog.Product);
                }
            }
        }

        private async Task UpdateProductAsync(int id, ProductDto product)
        {
            var api = new ApiService();
            var request = new CreateProductRequest
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                DiscountPercentage = product.DiscountPercentage
            };
            await api.UpdateProductAsync(id, request);
            await LoadProductsAsync();
        }

        private void StockProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ProductDto product)
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox($"Enter quantity change for {product.Name}:", "Update Stock", "0");
                if (int.TryParse(input, out var change))
                {
                    var reason = Microsoft.VisualBasic.Interaction.InputBox("Enter reason:", "Stock Reason", "Manual adjustment");
                    _ = UpdateStockAsync(product.Id, change, reason);
                }
            }
        }

        private async Task UpdateStockAsync(int id, int change, string reason)
        {
            var api = new ApiService();
            await api.UpdateStockAsync(id, change, reason);
            await LoadProductsAsync();
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ProductDto product)
            {
                if (MessageBox.Show($"Delete {product.Name}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var api = new ApiService();
                    await api.DeleteProductAsync(product.Id);
                    await LoadProductsAsync();
                }
            }
        }
    }
}
