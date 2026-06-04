using EcoHub.Shared.Constants;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace EcoHub.Admin.Services
{
    public class SignalRService
    {
        private HubConnection? _connection;

        public event Action<NotificationDto>? OnNotificationReceived;
        public event Action? OnConnected;
        public event Action? OnDisconnected;

        public async Task StartAsync()
        {
            var url = "https://localhost:7086" + ApiRoutes.SignalR.Hub;
            _connection = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(AppState.AuthToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<NotificationDto>("ReceiveNotification", notification =>
            {
                OnNotificationReceived?.Invoke(notification);
            });

            _connection.Reconnecting += _ =>
            {
                OnDisconnected?.Invoke();
                return Task.CompletedTask;
            };

            _connection.Reconnected += _ =>
            {
                OnConnected?.Invoke();
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            await _connection.InvokeAsync("JoinGroup", "Admins");
            OnConnected?.Invoke();
        }

        public async Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                OnDisconnected?.Invoke();
            }
        }

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;
    }
}
