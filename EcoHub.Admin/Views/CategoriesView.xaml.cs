using EcoHub.Admin.Services;
using EcoHub.Shared.Models;
using System.Windows;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class CategoriesView : UserControl
    {
        public CategoriesView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            var api = new ApiService();
            var categories = await api.GetCategoriesAsync();
            CategoriesGrid.ItemsSource = categories;
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var api = new ApiService();
            var dto = new CategoryDto { Name = NewCategoryName.Text, Description = NewCategoryDesc.Text };
            await api.CreateCategoryAsync(dto);
            await LoadCategoriesAsync();
            NewCategoryName.Text = "Name";
            NewCategoryDesc.Text = "Description";
        }

        private async void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CategoryDto category)
            {
                var name = Microsoft.VisualBasic.Interaction.InputBox("New name:", "Edit Category", category.Name);
                var desc = Microsoft.VisualBasic.Interaction.InputBox("New description:", "Edit Category", category.Description);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var api = new ApiService();
                    await api.UpdateCategoryAsync(category.Id, new CategoryDto { Name = name, Description = desc });
                    await LoadCategoriesAsync();
                }
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CategoryDto category)
            {
                if (MessageBox.Show($"Delete {category.Name}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var api = new ApiService();
                    await api.DeleteCategoryAsync(category.Id);
                    await LoadCategoriesAsync();
                }
            }
        }
    }
}
