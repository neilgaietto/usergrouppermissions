/// 
/// Copy of internal umbraco class: \umbraco.core\Models\Rdbms\User2NodePermissionDto.cs
/// 

using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace UserGroupPermissions.Models
{
    [TableName("umbracoUser2NodePermission")]
    [PrimaryKey("userId", autoIncrement = false)]
    [ExplicitColumns]
    internal class User2NodePermissionDto
    {
        [Column("userId")]
        [PrimaryKeyColumn(AutoIncrement = false, Name = "PK_umbracoUser2NodePermission", OnColumns = "userId, nodeId, permission")]
        public int UserId { get; set; }

        [Column("nodeId")]
        public int NodeId { get; set; }

        [Column("permission")]
        public string Permission { get; set; }
    }
}