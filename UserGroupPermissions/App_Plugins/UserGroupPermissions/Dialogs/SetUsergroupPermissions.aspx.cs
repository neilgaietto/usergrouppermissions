using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using umbraco;
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
    public partial class SetUsergroupPermissions : UmbracoEnsuredPage
    {
        private ArrayList permissions = new ArrayList();
        private IContent node;

        private readonly UserTypePermissionsService _userTypePermissionsService;

        public SetUsergroupPermissions()
        {
            _userTypePermissionsService = new UserTypePermissionsService();
        }

        protected void Page_Load(object sender, System.EventArgs e)
        {
            var service = ApplicationContext.Services.TextService;
            var culture = CultureInfo.GetCultureInfo(GlobalSettings.DefaultUILanguage);
            btnUpdate.Text = service.Localize("update", culture);
            pane_form.Text = "Set Usergroup permissions for the page " + node.Name;
        }

       
        #region Web Form Designer generated code
        override protected void OnInit(EventArgs e)
        {
            //
            // CODEGEN: This call is required by the ASP.NET Web Form Designer.
            //
            InitializeComponent();
            base.OnInit(e);

            var textService = ApplicationContext.Services.TextService;
            var userService = ApplicationContext.Services.UserService;
            var contentService = ApplicationContext.Services.ContentService;
            var nodeId = int.Parse(Request.GetItemAsString("id"));
            node = contentService.GetById(nodeId);
            var user = Security.CurrentUser;
            var culture = user.GetUserCulture(textService);


            HtmlTable ht = new HtmlTable();
            ht.Attributes.Add("class", "table");


            HtmlTableRow captions = new HtmlTableRow();
            captions.Cells.Add(new HtmlTableCell());


            foreach (IUserType userType in userService.GetAllUserTypes())
            {
                if (userType.Id > 0 && userType.Alias != "admin")
                {
                    HtmlTableCell hc = new HtmlTableCell("th");

                    hc.Controls.Add(new LiteralControl(userType.Name));
                    captions.Cells.Add(hc);
                }
            }
            ht.Rows.Add(captions);
            foreach (umbraco.interfaces.IAction a in umbraco.BusinessLogic.Actions.Action.GetAll())
            {
                if (a.CanBePermissionAssigned)
                {
                    var compoundKey = string.Format("actions/{0}", a.Alias);
                    HtmlTableRow hr = new HtmlTableRow();

                    HtmlTableCell hc = new HtmlTableCell();
                    hc.Attributes.Add("class", "guiDialogTinyMark");
                    hc.Controls.Add(new LiteralControl(textService.Localize(compoundKey, culture)));
                    hr.Cells.Add(hc);


                    foreach (IUserType userType in ApplicationContext.Services.UserService.GetAllUserTypes())
                    {
                        // Not disabled users and not system account
                        if (userType.Id > 0 && userType.Alias != "admin")
                        {
                            CheckBox c = new CheckBox();
                            c.ID = userType.Id + "_" + a.Letter;
                            if (_userTypePermissionsService.GetPermissions(userType, node.Path).IndexOf(a.Letter) > -1)
                                    c.Checked = true;
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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {

        }
        #endregion

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
                    //Replace permissions on all users
                    _userTypePermissionsService.CopyPermissions(usertype, node);
                }
            }


            // Sync the tree
            ClientTools.SyncTree(node.Path, true);

            // Update feedback message
            feedback1.type = umbraco.uicontrols.Feedback.feedbacktype.success;
            feedback1.Text = "Usergroup permissions saved ok";
            pane_form.Visible = false;
            phButtons.Visible = false;
            
        }
    }
}