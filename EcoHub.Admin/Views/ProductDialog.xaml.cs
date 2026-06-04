using EcoHub.Shared.Models;
using System.Windows;
using System.Windows.Input;

namespace EcoHub.Admin.Views
{
    public partial class ProductDialog : Window
    {
        public ProductDto Product { get; private set; }

        public ProductDialog(ProductDto product)
        {
            InitializeComponent();
            Product = product;
            NameBox.Text = product.Name;
            DescriptionBox.Text = product.Description;
            PriceBox.Text = product.Price.ToString();
            StockBox.Text = product.StockQuantity.ToString();
            CategoryBox.Text = product.CategoryId.ToString();
            DiscountBox.Text = product.DiscountPercentage.ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(PriceBox.Text, out var price) && int.TryParse(StockBox.Text, out var stock) && int.TryParse(CategoryBox.Text, out var catId) && decimal.TryParse(DiscountBox.Text, out var discount))
            {
                Product.Name = NameBox.Text;
                Product.Description = DescriptionBox.Text;
                Product.Price = price;
                Product.StockQuantity = stock;
                Product.CategoryId = catId;
                Product.DiscountPercentage = discount;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid numeric values.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }
    }
}
