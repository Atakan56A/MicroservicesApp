namespace Authorization.API.Models
{
    public class AssignPermissionsRequest
    {
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }
}
