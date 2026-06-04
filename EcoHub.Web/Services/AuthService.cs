using EcoHub.Shared.Constants;
using EcoHub.Shared.Models;
using EcoHub.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcoHub.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly LocalStorageService _localStorage;
        private readonly JwtAuthStateProvider _authState;

        public event Action? AuthStateChanged;

        public AuthService(HttpClient http, LocalStorageService localStorage, AuthenticationStateProvider authState)
        {
            _http = http;
            _localStorage = localStorage;
            _authState = (JwtAuthStateProvider)authState;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest { Email = email, Password = password });
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result != null)
            {
                await _localStorage.SetItemAsync("auth_token", result.Token);
                _authState.NotifyAuthenticationStateChanged();
                AuthStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var response = await _http.PostAsJsonAsync(ApiRoutes.Auth.Register, request);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result != null)
            {
                await _localStorage.SetItemAsync("auth_token", result.Token);
                _authState.NotifyAuthenticationStateChanged();
                AuthStateChanged?.Invoke();
                return true;
            }
            return false;
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("auth_token");
            _authState.NotifyAuthenticationStateChanged();
            AuthStateChanged?.Invoke();
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _localStorage.GetItemAsync("auth_token");
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.GetAsync(ApiRoutes.Auth.Me);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }
        public async Task<UserDto?> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var response = await _http.PutAsJsonAsync(ApiRoutes.Auth.UpdateProfile, request);
            if (!response.IsSuccessStatusCode) return null;
            var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
            AuthStateChanged?.Invoke();
            return updatedUser;
        }
    }
}
