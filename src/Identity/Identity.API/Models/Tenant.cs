namespace Identity.API.Models
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
