namespace UserGroupPermissions.Models
{
    public class SetPermissionsRequest
    {
        public int NodeId { get; set; }
        public UserTypePermission[] UserTypePermissions { get; set; }
        public bool ReplacePermissionsOnUsers { get; set; }
        public bool IgnoreBasePermissions { get; set; }
        public bool ReplaceChildNodePermissions { get; set; }
    }
}