namespace UserGroupPermissions.ExtensionMethods
{

    // Namespaces.
    using Umbraco.Core.Models.Membership;


    /// <summary>
    /// Extension methods for users.
    /// </summary>
    public static class UserExtensions
    {

        #region Extension Methods

        /// <summary>
        /// Is the specified user an administrator?
        /// </summary>
        /// <param name="user">
        /// The user.
        /// </param>
        /// <returns>
        /// True, if the specified user is an administrator; otherwise, false.
        /// </returns>
        public static bool IsAdmin(this IUser user)
        {
            return user.UserType.IsAdmin();

        }


        /// <summary>
        /// Is the specified user disabled?
        /// </summary>
        /// <param name="user">
        /// The user.
        /// </param>
        /// <returns>
        /// True, if the specified user is disabled; otherwise, false.
        /// </returns>
        public static bool Disabled(this IUser user)
        {
            return !user.IsApproved;
        }

        #endregion

    }

}