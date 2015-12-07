﻿using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using UserGroupPermissions.MenuActions;

namespace UserGroupPermissions.ExtensionMethods
{
    public static class UserTypeExtensions
    {
        /// <summary>
        /// Gets all users related to the doctype
        /// </summary>
        /// <returns></returns>
        public static IUser[] GetAllRelatedUsers(this IUserType userType)
        {
            int total;

            return ApplicationContext.Current.Services.UserService
                .GetAll(0, int.MaxValue, out total).Where(x=>x.UserType.Id== userType.Id)
                .OrderBy(x=>x.Name).ToArray();

        }

        public static bool IsAdmin(this IUserType userType)
        {
            return userType.Alias == "admin";

        }

        public static bool HasDisabledMediaUpload(this IUserType userType)
        {
            var currentlyDisabled = userType.Permissions.IndexOf(DisableMediaUploadPermissions.Instance.Letter.ToString()) >= 0;
            return currentlyDisabled;

        }
    }
}