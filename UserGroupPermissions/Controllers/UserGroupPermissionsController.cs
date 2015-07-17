namespace UserGroupPermissions.Controllers
{

    // Namespaces.
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
            var userService = Services.UserService;
            var user = userService.GetUserById(request.UserId);
            //TODO: ...
            return new
            {
                Success = true
            };
        }

        #endregion

    }

}