using EcoHub.Shared.Constants;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace EcoHub.Web.Services
{
    public class SignalRService
    {
        private HubConnection? _connection;
        private readonly AuthService _authService;

        public event Action<NotificationDto>? OnNotificationReceived;
        public event Action<int, string>? OnOrderStatusUpdated;

        public SignalRService(AuthService authService)
        {
            _authService = authService;
        }

        public async Task StartAsync()
        {
            var token = await _authService.GetTokenAsync();
            var url = "https://localhost:7086" + ApiRoutes.SignalR.Hub;

            _connection = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<NotificationDto>("ReceiveNotification", notification =>
            {
                OnNotificationReceived?.Invoke(notification);
            });

            _connection.On("OrderStatusUpdated", (int orderId, string status) =>
            {
                OnOrderStatusUpdated?.Invoke(orderId, status);
            });

            await _connection.StartAsync();

            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
            {
                await _connection.InvokeAsync("JoinGroup", $"User_{user.Id}");
            }
        }

        public async Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
            }
        }
    }
}
