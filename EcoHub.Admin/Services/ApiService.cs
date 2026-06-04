using EcoHub.Shared.Constants;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcoHub.Admin.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public ApiService()
        {
            _http = new HttpClient { BaseAddress = new Uri("https://localhost:7086/") };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(AppState.AuthToken))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppState.AuthToken);
        }

        public async Task<AuthResponse?> LoginAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest { Email = email, Password = password });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }

        public async Task<DashboardStatsDto?> GetDashboardStatsAsync(DateTime? since = null)
        {
            SetAuthHeader();
            var url = ApiRoutes.Dashboard.Stats;
            if (since.HasValue) url += $"?since={since.Value:o}";
            return await _http.GetFromJsonAsync<DashboardStatsDto>(url);
        }

        public async Task<List<UserDto>?> GetUsersAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<UserDto>>(ApiRoutes.Users.GetAll);
        }

        public async Task<List<UserDto>?> GetNewUsersAsync(DateTime since)
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<UserDto>>($"{ApiRoutes.Users.GetNew}?since={since:o}");
        }

        public async Task<List<ProductDto>?> GetProductsAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<ProductDto>>(ApiRoutes.Products.GetAll);
        }

        public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync(ApiRoutes.Products.GetAll, request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProductDto>();
        }

        public async Task<bool> UpdateProductAsync(int id, CreateProductRequest request)
        {
            SetAuthHeader();
            var response = await _http.PutAsJsonAsync($"api/products/{id}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            SetAuthHeader();
            var response = await _http.DeleteAsync($"api/products/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateStockAsync(int id, int quantityChange, string reason)
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync($"api/products/{id}/stock", new UpdateStockRequest { QuantityChange = quantityChange, Reason = reason });
            return response.IsSuccessStatusCode;
        }

        public async Task<List<CategoryDto>?> GetCategoriesAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<CategoryDto>>(ApiRoutes.Categories.GetAll);
        }

        public async Task<CategoryDto?> CreateCategoryAsync(CategoryDto dto)
        {
            SetAuthHeader();
            var response = await _http.PostAsJsonAsync(ApiRoutes.Categories.GetAll, dto);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CategoryDto>();
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            SetAuthHeader();
            var response = await _http.PutAsJsonAsync($"api/categories/{id}", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            SetAuthHeader();
            var response = await _http.DeleteAsync($"api/categories/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<OrderDto>?> GetOrdersAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<OrderDto>>(ApiRoutes.Orders.GetAll);
        }

        public async Task<List<OrderDto>?> GetNewOrdersAsync(DateTime since)
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<OrderDto>>($"{ApiRoutes.Orders.GetNew}?since={since:o}");
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, OrderStatus status)
        {
            SetAuthHeader();
            var response = await _http.PutAsJsonAsync($"api/orders/{id}/status", new UpdateOrderStatusRequest { Status = status });
            return response.IsSuccessStatusCode;
        }

        public async Task<List<NotificationDto>?> GetNotificationsAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<NotificationDto>>(ApiRoutes.Notifications.GetAll);
        }

        public async Task MarkNotificationReadAsync(int id)
        {
            SetAuthHeader();
            await _http.PutAsync($"api/notifications/{id}/read", null);
        }

        public async Task MarkAllNotificationsReadAsync()
        {
            SetAuthHeader();
            await _http.PutAsync("api/notifications/read-all", null);
        }

        public async Task<List<SystemSettingDto>?> GetSettingsAsync()
        {
            SetAuthHeader();
            return await _http.GetFromJsonAsync<List<SystemSettingDto>>(ApiRoutes.Settings.GetAll);
        }

        public async Task<bool> UpdateSettingAsync(string key, string value)
        {
            SetAuthHeader();
            var response = await _http.PutAsync($"api/settings/{key}", new StringContent($"\"{value}\"", System.Text.Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<byte[]?> DownloadOrderPdfAsync(int orderId)
        {
            SetAuthHeader();
            var response = await _http.GetAsync($"api/reports/order/{orderId}/pdf");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
