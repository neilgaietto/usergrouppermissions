using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Persistence;

namespace UserGroupPermissions.Models
{
    [TableName("UserTypePermissions")]
    public class UserTypePermission
    {
        public int NodeId { get; set; }
        public int UserTypeId { get; set; }
        // Should be a char, but changed to string due to Umbraco bug: http://issues.umbraco.org/issue/U4-6787
        public string PermissionId { get; set; }
    }
}