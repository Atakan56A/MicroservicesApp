namespace Authorization.API.Models
{
    public class ValidationRequest
    {
        public Guid UserId { get; set; }
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
