namespace UserGroupPermissions.Controllers
{

    // Namespaces.
    using Businesslogic;
    using Models;
    using System.Web.Http;
    using Umbraco.Web;
    using Umbraco.Web.Editors;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.WebApi.Filters;
    using Constants = Umbraco.Core.Constants;


    /// <summary>
    /// Controller for user group permission operations that occur on the server.
    /// </summary>
    [PluginController("UGP")]
    [UmbracoApplicationAuthorize(Constants.Applications.Users)]
    public class UserGroupPermissionsController : UmbracoAuthorizedJsonController
    {

        #region Constants

        private const string UserNotFound = "The specified user was not found.";

        #endregion


        #region Readonly Variables

        private readonly UserTypePermissionsService _userTypePermissionsService;

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public UserGroupPermissionsController() : this(UmbracoContext.Current)
        {
        }


        /// <summary>
        /// Primary constructor.
        /// </summary>
        public UserGroupPermissionsController(UmbracoContext umbracoContext)
            : base(umbracoContext)
        {
            _userTypePermissionsService = new UserTypePermissionsService();
        }

        #endregion


        #region Web Methods

        /// <summary>
        /// Applies all user group permissions for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>
        /// An object indicating the success (or failure) of the operation.
        /// </returns>
        [HttpPost]
        public object ApplyAllGroupPermissions(ApplyRequest request)
        {

            // Variables.
            var failureReason = default(string);
            var userService = Services.UserService;
            var user = userService.GetUserById(request.UserId);
            var success = true;


            // User found?
            if (user == null)
            {
                success = false;
                failureReason = UserNotFound;
            }


            // Copy permissions.
            if (success)
            {
                _userTypePermissionsService.CopyPermissionsForSingleUser(user);
            }


            // Indicate success or failure.
            if (success)
            {
                return new
                {
                    Success = true
                };
            }
            else
            {
                return new
                {
                    Success = false,
                    Reason = failureReason
                };
            }

        }

        #endregion

    }

}