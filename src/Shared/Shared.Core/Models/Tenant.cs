namespace MicroservicesApp.Shared.Models
{
    public class Tenant
    {
        public int Id { get; set; }  // int olarak standart belirledik
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}