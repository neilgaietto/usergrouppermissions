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
        
        // The event listener method
        void TreeControllerBase_MenuRendering(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            // Add a custom menu item for all admin users for all content tree nodes.
            IUser currentUser = sender.Security.CurrentUser;
            if (sender.TreeAlias == "content" && currentUser.IsAdmin())
            {
                var menuItem = new MenuItem("UserGroupPermissions", "User Group Permissions") {Icon = "vcard"};

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