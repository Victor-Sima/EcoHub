using System.Net.Http.Headers;

namespace EcoHub.Web.Services
{
    public class AuthHttpHandler : DelegatingHandler
    {
        private readonly LocalStorageService _localStorage;

        public AuthHttpHandler(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}