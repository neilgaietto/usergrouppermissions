using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using umbraco;
using umbraco.interfaces;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Web;
using Umbraco.Web.UI.Pages;
using UserGroupPermissions.Businesslogic;

namespace UserGroupPermissions.Dialogs
{

    /// <summary>
    /// Helps with user group permissions.
    /// </summary>
    /// <remarks>
    /// "Borrowed" from the core: https://github.com/umbraco/Umbraco-CMS/blob/release-7.2.6/src/Umbraco.Web/umbraco.presentation/umbraco/dialogs/cruds.aspx.cs
    /// </remarks>
    public partial class SetUserGroupPermissions : UmbracoEnsuredPage
    {
        private ArrayList permissions = new ArrayList();
        private IContent node;

        private readonly UserTypePermissionsService _userTypePermissionsService;

        public SetUserGroupPermissions()
        {
            _userTypePermissionsService = new UserTypePermissionsService();
        }

        protected void Page_Load(object sender, System.EventArgs e)
        {
            var service = ApplicationContext.Services.TextService;
            var culture = CultureInfo.GetCultureInfo(GlobalSettings.DefaultUILanguage);
            btnUpdate.Text = service.Localize("update", culture);
            pane_form.Text = "Set user group permissions for the page " + node.Name;
        }

        override protected void OnInit(EventArgs e)
        {
            base.OnInit(e);

            var textService = ApplicationContext.Services.TextService;
            var userService = ApplicationContext.Services.UserService;
            var contentService = ApplicationContext.Services.ContentService;
            var nodeId = int.Parse(Request.GetItemAsString("id"));
            node = contentService.GetById(nodeId);
            var user = Security.CurrentUser;
            var culture = user.GetUserCulture(textService);
            var orderedUserTypes = userService.GetAllUserTypes().OrderBy(x => x.Name);


            HtmlTable ht = new HtmlTable();
            ht.Attributes.Add("class", "table");


            HtmlTableRow captions = new HtmlTableRow();
            captions.Cells.Add(new HtmlTableCell());

            foreach (IUserType userType in orderedUserTypes)
            {
                if (userType.Id > 0 && userType.Alias != "admin")
                {
                    HtmlTableCell hc = new HtmlTableCell("th");

                    hc.Controls.Add(new LiteralControl(userType.Name));
                    captions.Cells.Add(hc);
                }
            }
            ht.Rows.Add(captions);

            // Would like to replace the call to Action.GetAll(), but this is currently
            // my best option: https://our.umbraco.org/forum/umbraco-7/developing-umbraco-7-packages/67584-replacement-for-umbracobusinesslogicactionsactiongetall
            var actions = umbraco.BusinessLogic.Actions.Action.GetAll()
                .Cast<IAction>().OrderBy(x => NameForAction(x, user));
            foreach (IAction a in actions)
            {
                if (a.CanBePermissionAssigned)
                {
                    var compoundKey = string.Format("actions/{0}", a.Alias);
                    HtmlTableRow hr = new HtmlTableRow();

                    HtmlTableCell hc = new HtmlTableCell();
                    hc.Attributes.Add("class", "guiDialogTinyMark");
                    hc.Controls.Add(new LiteralControl(textService.Localize(compoundKey, culture)));
                    hr.Cells.Add(hc);


                    foreach (IUserType userType in orderedUserTypes)
                    {
                        // Not disabled users and not system account
                        if (userType.Id > 0 && userType.Alias != "admin")
                        {
                            CheckBox c = new CheckBox();
                            c.ID = userType.Id + "_" + a.Letter;
                            var hasPermission = _userTypePermissionsService
                                .GetPermissions(userType, node.Path).IndexOf(a.Letter) > -1;
                            c.Checked = hasPermission;
                            HtmlTableCell cell = new HtmlTableCell();
                            cell.Style.Add("text-align", "center");
                            cell.Controls.Add(c);
                            permissions.Add(c);
                            hr.Cells.Add(cell);
                        }

                    }
                    ht.Rows.Add(hr);
                }

            }

            ClientTools.SyncTree(node.Path, true);

            phControls.Controls.Add(ht);

        }

        protected void btnUpdate_Click(object sender, System.EventArgs e)
        {
            var allUserTypes = new Dictionary<int, string>();
            var userService = ApplicationContext.Services.UserService;

            foreach (var u in userService.GetAllUserTypes())
            {
                allUserTypes.Add(u.Id, "");
            }

            foreach (CheckBox c in permissions)
            {
                // Update the user with the new permission
                if (c.Checked)
                {
                    var posUnderscore = c.ID.IndexOf("_");
                    var typeId = int.Parse(c.ID.Substring(0, posUnderscore));
                    var strPermissions = c.ID.Substring(posUnderscore + 1, c.ID.Length - posUnderscore - 1);
                    var newPermissions = (allUserTypes[typeId] + strPermissions).ToCharArray();
                    var distinctPermissions = newPermissions.DistinctBy(x => x.ToString().ToLower());
                    var combinedPermissions = string.Join(string.Empty, distinctPermissions);
                    allUserTypes[typeId] = combinedPermissions;
                }
            }


            // Loop through the users and update their Cruds...
            foreach (var pair in allUserTypes)
            {
                string cruds = "-";
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    cruds = pair.Value;
                }
                IUserType usertype = userService.GetUserTypeById(pair.Key);

                _userTypePermissionsService.UpdateCruds(usertype, node, cruds);

                if (ReplacePermissionsOnUsers.Checked)
                {
                    // Replace permissions on all users
                    _userTypePermissionsService.CopyPermissions(usertype, node);
                }
            }


            // Sync the tree
            ClientTools.SyncTree(node.Path, true);

            // Update feedback message
            feedback1.type = umbraco.uicontrols.Feedback.feedbacktype.success;
            feedback1.Text = "User group permissions saved successfully.";
            pane_form.Visible = false;
            phButtons.Visible = false;

        }

        // Attempts to translation an action alias into a name in the user's current language.
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

    }

}