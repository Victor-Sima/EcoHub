using EcoHub.Admin.Services;
using EcoHub.Shared.Models;
using System.Windows;
using System.Windows.Controls;

namespace EcoHub.Admin.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            var api = new ApiService();
            var settings = await api.GetSettingsAsync();
            SettingsGrid.ItemsSource = settings;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var api = new ApiService();
            if (SettingsGrid.ItemsSource is List<SystemSettingDto> settings)
            {
                foreach (var setting in settings)
                {
                    await api.UpdateSettingAsync(setting.Key, setting.Value);
                }
                MessageBox.Show("Settings saved.");
            }
        }
    }
}
