using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.BusinessLogic;
using umbraco.DataLayer;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;

namespace UserGroupPermissions.ExtensionMethods
{
    public static class UserExtensions
    {
        public static bool IsAdmin(this IUser user)
        {
            return user.UserType.IsAdmin();

        }

        public static bool Disabled(this IUser user)
        {
            return !user.IsApproved;
        }
    }
}