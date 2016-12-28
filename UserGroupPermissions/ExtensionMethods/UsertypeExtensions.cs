namespace UserGroupPermissions.ExtensionMethods
{

    // Namespaces.
    using Umbraco.Core.Models.Membership;


    /// <summary>
    /// Extension methods for user types.
    /// </summary>
    public static class UserTypeExtensions
    {

        #region Extension Methods

        /// <summary>
        /// Is the specified user type an administrator?
        /// </summary>
        /// <param name="userType">
        /// The user type.
        /// </param>
        /// <returns>
        /// True, if the specified user is an administrator; otherwise, false.
        /// </returns>
        public static bool IsAdmin(this IUserType userType)
        {
            return userType.Alias == "admin";
        }

        #endregion

    }

}