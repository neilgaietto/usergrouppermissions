namespace UserGroupPermissions.Events
{

    // Namespaces.
    using ExtensionMethods;
    using Umbraco.Core;
    using Umbraco.Web.Models.Trees;
    using Umbraco.Web.Trees;


    /// <summary>
    /// Adds the "Apply User Group Permissions" to the context menu for a user.
    /// </summary>
    public class AddApplyUserGroupPermissionsToContextMenu : ApplicationEventHandler
    {

        #region Constants

        private const string DialogViewPath = "/App_Plugins/UserGroupPermissions/Views/ApplyUserGroupPermissions.html";
        private const string DialogTitle = "Apply User Group Permissions";

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AddApplyUserGroupPermissionsToContextMenu()
        {
            TreeControllerBase.MenuRendering += TreeControllerBase_MenuRendering;
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// Event handler when tree is rendering.
        /// </summary>
        void TreeControllerBase_MenuRendering(TreeControllerBase sender, MenuRenderingEventArgs e)
        {

            // Variables.
            var userService = ApplicationContext.Current.Services.UserService;
            var currentUser = sender.Security.CurrentUser;
            var treeType = e.QueryStrings.Get("treeType");
            var section = e.QueryStrings.Get("section");
            var validTree = "users".InvariantEquals(treeType) && "users".InvariantEquals(section);
            var strUserId = e.NodeId ?? string.Empty;
            var userId = default(int);
            var validUserId = int.TryParse(strUserId, out userId)
                ? userId > 0
                : false;
            var user = validUserId
                ? userService.GetUserById(userId)
                : null;
            var validUser = user != null && !user.IsAdmin();
            var validCurrentUser = currentUser.IsAdmin();
            var shouldAddMenuItem = validTree && validCurrentUser && validUserId && validUser;


            // Add the menu item?
            if (shouldAddMenuItem)
            {
                var menuItem = new MenuItem("ApplyUserGroupPermissions", "Apply Group Permissions")
                {
                    Icon = "vcard"
                };
                menuItem.LaunchDialogView(DialogViewPath, DialogTitle);
                e.Menu.Items.Add(menuItem);
            }

        }

        #endregion

    }

}