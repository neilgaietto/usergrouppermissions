namespace UserGroupPermissions.Models
{
    using Umbraco.Core.Persistence;
    [TableName("UserTypePermissions")]
    public class UserTypePermissionRow
    {
        public int NodeId { get; set; }
        public int UserTypeId { get; set; }
        // Should be a char, but changed to string due to Umbraco bug: http://issues.umbraco.org/issue/U4-6787
        public string PermissionId { get; set; }
    }
}