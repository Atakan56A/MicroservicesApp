using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Authorization.API.Services
{
    public interface IIdentityService
    {
        Task<bool> ValidateUserAsync(string userId, string token);
        Task<string> GetUserIdFromUsernameAsync(string username);
    }

    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public IdentityService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<bool> ValidateUserAsync(string userId, string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync($"api/auth/validate-user/{userId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetUserIdFromUsernameAsync(string username)
        {
            var response = await _httpClient.GetFromJsonAsync<UserIdResponse>($"api/auth/get-user-id?username={username}");
            return response?.UserId;
        }

        private class UserIdResponse
        {
            public string UserId { get; set; }
        }
    }
}