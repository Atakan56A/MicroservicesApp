namespace Identity.API.Models.Requests
{
    public class RefreshTokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
