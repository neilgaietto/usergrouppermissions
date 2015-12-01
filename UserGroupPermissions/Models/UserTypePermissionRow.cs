namespace UserGroupPermissions.Models
{
    using Umbraco.Core.Persistence;
    using Umbraco.Core.Persistence.DatabaseAnnotations;
    [TableName("UserTypePermissions")]
    [PrimaryKey("AutoId", autoIncrement = true)]
    public class UserTypePermissionRow
    {
        [PrimaryKeyColumn(AutoIncrement = true, Clustered = true)]
        public long AutoId { get; set; }
        public int NodeId { get; set; }
        public int UserTypeId { get; set; }
        // Should be a char, but changed to string due to Umbraco bug: http://issues.umbraco.org/issue/U4-6787
        public string PermissionId { get; set; }
    }
}