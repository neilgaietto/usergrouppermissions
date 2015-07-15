using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;
using UserGroupPermissions.ExtensionMethods;

namespace UserGroupPermissions.Events
{
    public class AddUserGroupPermissionToContextMenu : ApplicationEventHandler 
    {
        public AddUserGroupPermissionToContextMenu()
        {
            TreeControllerBase.MenuRendering += TreeControllerBase_MenuRendering;
        }
        
        // A context menu is rendering.
        void TreeControllerBase_MenuRendering(TreeControllerBase sender, MenuRenderingEventArgs e)
        {

            // Initial check (in content tree and on a node).
            IUser currentUser = sender.Security.CurrentUser;
            var showMenuItem = sender.TreeAlias == "content";
            var strNodeId = e.NodeId;
            var nodeId = default(int);
            if (showMenuItem && int.TryParse(strNodeId, out nodeId))
            {
                showMenuItem = nodeId >= 0;
            }
            else
            {
                showMenuItem = false;
            }

            // Check permissions for non-admin users.
            if (showMenuItem && !currentUser.IsAdmin())
            {
                showMenuItem = false;
                var userService = ApplicationContext.Current.Services.UserService;
                var permissions = userService.GetPermissions(currentUser, nodeId);
                var letter = MenuActions.UserGroupPermissions.Instance.Letter.ToString();
                if(permissions.Any(x => x.AssignedPermissions.InvariantContains(letter)))
                {
                    showMenuItem = true;
                }
            }

            // Add a custom menu item in the content tree.
            if (showMenuItem)
            {

                var menuItem = new MenuItem("UserGroupPermissions", "User Group Permissions")
                {
                    Icon = "vcard"
                };

                menuItem.LaunchDialogUrl("/App_Plugins/UserGroupPermissions/Dialogs/SetUserGroupPermissions.aspx?id=" + e.NodeId, "User Group Permissions");

                var permissionsIndex = e.Menu.Items.FindIndex(x =>
                    "Permissions".InvariantEquals(x.Name) ||
                    "Permissions".InvariantEquals(x.Alias));

                // Attempt to insert after the existing "Permissions" menu item.
                if (permissionsIndex >= 0)
                {
                    e.Menu.Items.Insert(permissionsIndex + 1, menuItem);
                }
                else
                {
                    e.Menu.Items.Add(menuItem);
                }

            }

        }

    }
}