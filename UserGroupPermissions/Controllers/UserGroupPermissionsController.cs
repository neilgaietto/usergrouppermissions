using UserGroupPermissions.ExtensionMethods;
using UserGroupPermissions.MenuActions;

namespace UserGroupPermissions.Controllers
{

    // Namespaces.
    using Businesslogic;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;
    using umbraco.interfaces;
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.Membership;
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
        /// <param name="request">
        /// The request parameters.
        /// </param>
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


        /// <summary>
        /// Applies the user group permissions to the specified node.
        /// </summary>
        /// <param name="request">
        /// The request parameters.
        /// </param>
        /// <returns>
        /// An object indicating the success (or failure) of the operation.
        /// </returns>
        [HttpPost]
        public object SetGroupPermissions(SetPermissionsRequest request)
        {

            // Variables.
            var userService = Services.UserService;
            var contentService = ApplicationContext.Services.ContentService;
            var userPermissions = request.UserTypePermissions;
            var nodeId = request.NodeId;
            var ignoreBase = request.IgnoreBasePermissions;
            var replaceChildPermissions = request.ReplaceChildNodePermissions;
            var node = contentService.GetById(nodeId);
            var permissionsByTypeId = new Dictionary<int, string[]>();
            var assignablePermissions = ActionsResolver.Current.Actions.Where(x => x.CanBePermissionAssigned).Select(x => x.Letter);


            // Add all user types to dictionary.
            foreach (var u in userService.GetAllUserTypes())
            {
                //check if any permissions were set
                var newUserTypePermissions = userPermissions.FirstOrDefault(x => x.UserTypeId == u.Id);
                if (newUserTypePermissions == null) continue;

                //grab base usertype permissions for reference - filters no assignable
                var basePermissions = u.Permissions.Where(x => assignablePermissions.Contains(x[0])).ToList();

                //grab new permissions
                var permissions = newUserTypePermissions.Permissions.ToList();
                if (!permissions.Any() && !(ignoreBase && !basePermissions.Any()))
                {
                    //no permissions set means disable all permissions
                    permissions.Add("-");
                }

                //if we ignore when set permissions match base permissions
                if (ignoreBase && permissions.All(x => basePermissions.Contains(x)) && basePermissions.All(x => permissions.Contains(x)))
                {
                    permissions = new List<string>();
                }


                //if we have any permissions set
                permissionsByTypeId[u.Id] = permissions.ToArray();

            }


            // Process each user type with permissions.
            foreach (var pair in permissionsByTypeId)
            {

                // Variables.
                var userType = userService.GetUserTypeById(pair.Key);


                // Update user type permissions.
                _userTypePermissionsService.UpdateCruds(userType, node, pair.Value.Select(x => x[0]), replaceChildPermissions);


                // Update user permissions?
                if (request.ReplacePermissionsOnUsers)
                {
                    _userTypePermissionsService.CopyPermissions(userType, node, replaceChildPermissions);
                }

            }


            // Indicate success.
            return new
            {
                Success = true
            };

        }


        /// <summary>
        /// Returns user group permissions for the specified node.
        /// </summary>
        /// <param name="request">
        /// The request parameters
        /// </param>
        /// <returns>
        /// The permissions.
        /// </returns>
        [HttpGet]
        [DisableBrowserCache]
        public object GetGroupPermissions([FromUri] GetPermissionsRequest request)
        {

            // Variables.
            var userService = ApplicationContext.Services.UserService;
            var contentService = ApplicationContext.Services.ContentService;
            var node = contentService.GetById(request.NodeId);
            var nodePath = node.Path;
            var user = Security.CurrentUser;
            var orderedUserTypes = userService.GetAllUserTypes()
                .Where(x => x.Id > 0 && !"admin".InvariantEquals(x.Alias))
                .OrderBy(x => x.Name);

            var orderedActions = ActionsResolver.Current.Actions
                .Where(x => x.CanBePermissionAssigned)
                .OrderBy(x => NameForAction(x, user));
            var permissionsByType = new Dictionary<int, string>();
            var actionTranslations = new Dictionary<string, string>();


            // Function to check if the specified type has the specified permission.
            var hasPermission = new Func<IUserType, char, bool>((ut, letter) =>
            {
                var permissions = default(string);
                var typeId = ut.Id;
                if (!permissionsByType.TryGetValue(typeId, out permissions))
                {
                    permissions = _userTypePermissionsService
                        .GetPermissions(ut, nodePath);
                    permissionsByType[typeId] = permissions;
                }
                return permissions.IndexOf(letter) > -1;
            });


            // Function to translate an action.
            var translateAction = new Func<IAction, string>(a =>
            {
                var alias = a.Alias;
                var actionTranslation = default(string);
                if (!actionTranslations.TryGetValue(alias, out actionTranslation))
                {
                    actionTranslation = NameForAction(a, user);
                    actionTranslations[alias] = actionTranslation;
                }
                return actionTranslation;
            });


            // Return permissions.
            return new
            {
                UserTypePermissions = orderedUserTypes.Select(ut => new
                {
                    UserTypeId = ut.Id,
                    Label = ut.Name,
                    Permissions = orderedActions.Select(a => new
                    {
                        Letter = a.Letter,
                        Label = translateAction(a),
                        HasPermission = hasPermission(ut, a.Letter)
                    }).ToArray()
                }).ToArray()
            };

        }

        [HttpPost]
        public object ApplyMediaUploadPermissions(ApplyMediaRequest request)
        {

            // Variables.
            var failureReason = default(string);
            var userService = Services.UserService;
            var role = userService.GetUserTypeById(request.RoleId);
            var success = true;


            // User found?
            if (role == null)
            {
                success = false;
                failureReason = UserNotFound;
            }


            // Copy permissions.
            if (success)
            {

                if (role.HasDisabledMediaUpload())
                {
                    role.Permissions =
                        role.Permissions.Where(x => x != DisableMediaUploadPermissions.Instance.Letter.ToString());
                }
                else
                {
                    role.Permissions = role.Permissions.Union(new[] { DisableMediaUploadPermissions.Instance.Letter.ToString() });

                }
                userService.SaveUserType(role, false);//does not trigger the saving event which would revert the toggle off
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


        #region Helper Methods

        /// <summary>
        /// Attempts to translate an action alias into a name in the user's current language.
        /// </summary>
        /// <param name="action">The menu action.</param>
        /// <param name="currentUser">The current user.</param>
        /// <returns>The translation.</returns>
        private string NameForAction(IAction action, IUser currentUser)
        {
            var service = ApplicationContext.Services.TextService;
            var culture = currentUser.GetUserCulture(service);
            var alias = action.Alias;
            var key = string.Format("actions/{0}", alias);
            var localized = service.Localize(key, culture);
            if (string.IsNullOrWhiteSpace(localized))
            {
                return alias;
            }
            else
            {
                if (localized.StartsWith("[") && localized.EndsWith("]"))
                {
                    return alias;
                }
            }
            return localized;
        }

        #endregion

    }

}