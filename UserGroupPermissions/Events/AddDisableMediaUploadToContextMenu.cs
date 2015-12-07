using Umbraco.Core;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;
using UserGroupPermissions.ExtensionMethods;
using UserGroupPermissions.MenuActions;

namespace UserGroupPermissions.Events
{
    public class AddDisableMediaUploadToContextMenu : ApplicationEventHandler
    {

        public AddDisableMediaUploadToContextMenu()
        {
            TreeControllerBase.MenuRendering += TreeControllerBase_MenuRendering;
        }

        void TreeControllerBase_MenuRendering(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            var user = sender.Security.CurrentUser;
            var path = "/App_Plugins/UserGroupPermissions/Views/ApplyMediaUploadPermissions.html";
            var title = "Media Upload Permissions";
            var treeType = e.QueryStrings.Get("treeType");
            var section = e.QueryStrings.Get("section");
            var validTree = "usertypes".InvariantEquals(treeType) && "users".InvariantEquals(section);
            var strRoleId = e.NodeId ?? "";
            var roleId = default(int);
            var validRoleId = int.TryParse(strRoleId, out roleId) && roleId > 0;
            var validRole = user.IsAdmin();
            if (validTree && validRoleId && validRole)
            {
                var targetRole = ApplicationContext.Current.Services.UserService.GetUserTypeById(roleId);
                var currentlyDisabled = targetRole.HasDisabledMediaUpload();
                var menuItem = new MenuItem("DisableMediaUploadPermissions", (currentlyDisabled ? "Enable" : "Disable") + " Media Upload Permissions")
                {
                    Icon = "vcard"
                };
                menuItem.LaunchDialogView(path, title);
                e.Menu.Items.Add(menuItem);


            }
        }

    }
}
