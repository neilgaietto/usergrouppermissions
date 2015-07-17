using Umbraco.Core;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;
using UserGroupPermissions.ExtensionMethods;

namespace UserGroupPermissions.Events
{
    public class AddApplyUserGroupPermissionsToContextMenu : ApplicationEventHandler
    {

        public AddApplyUserGroupPermissionsToContextMenu()
        {
            TreeControllerBase.MenuRendering += TreeControllerBase_MenuRendering;
        }

        void TreeControllerBase_MenuRendering(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            var user = sender.Security.CurrentUser;
            var path = "/App_Plugins/UserGroupPermissions/Views/ApplyUserGroupPermissions.html";
            var title = "Apply User Group Permissions";
            var treeType = e.QueryStrings.Get("treeType");
            var section = e.QueryStrings.Get("section");
            var validTree = "users".InvariantEquals(treeType) && "users".InvariantEquals(section);
            var strUserId = e.NodeId ?? "";
            var userId = default(int);
            var validUserId = int.TryParse(strUserId, out userId) ? userId > 0 : false;
            var validUser = user.IsAdmin();
            if (validTree && validUserId && validUser)
            {
                var menuItem = new MenuItem("ApplyUserGroupPermissions", "Apply Group Permissions")
                {
                    Icon = "vcard"
                };
                menuItem.LaunchDialogView(path, title);
                e.Menu.Items.Add(menuItem);
            }
        }

    }
}