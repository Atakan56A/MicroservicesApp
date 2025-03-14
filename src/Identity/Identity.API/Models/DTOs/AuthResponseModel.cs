namespace Identity.API.Models.DTOs
{
    public class AuthResponseModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public List<string> Roles { get; set; }
    }
}
