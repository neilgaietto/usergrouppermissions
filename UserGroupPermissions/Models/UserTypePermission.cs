namespace UserGroupPermissions.Models
{
    public class UserTypePermission
    {
        public int UserTypeId { get; set; }
        public string[] Permissions { get; set; }
    }
}