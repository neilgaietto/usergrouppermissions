<%@ Page MasterPageFile="~/umbraco/masterpages/umbracoDialog.Master"  Language="C#" AutoEventWireup="true" CodeBehind="SetUserGroupPermissions.aspx.cs" Inherits="UserGroupPermissions.Dialogs.SetUserGroupPermissions" %>
<%@ Register TagPrefix="ugp" Namespace="umbraco.uicontrols" Assembly="controls" %>
<%-- "Borrowed" from the core: https://github.com/umbraco/Umbraco-CMS/blob/7c4a189aa3cf583954defd9c43a3e55e325f2c3f/src/Umbraco.Web.UI/umbraco/dialogs/cruds.aspx --%>

<asp:Content ContentPlaceHolderID="head" runat="server">
<style>
  .guiDialogTinyMark{font-size: 9px !Important;}
</style>
</asp:Content>
<asp:Content ContentPlaceHolderID="body" runat="server">
    <div class="umb-dialog-body form-horizontal">
        <ugp:Feedback ID="feedback1" runat="server" />
        <ugp:Pane ID="pane_form" runat="server">
            <ugp:PropertyPanel runat="server">
                 <asp:PlaceHolder ID="phControls" runat="server"></asp:PlaceHolder>
            </ugp:PropertyPanel>
            <asp:CheckBox id="ReplacePermissionsOnUsers" runat="server" text="Replace permissions on all existing users for this node" />
        </ugp:Pane>
    </div>
    <asp:PlaceHolder runat="server" ID="phButtons">
        <div class="umb-dialog-footer btn-toolbar umb-btn-toolbar">
            <a href="#" class="btn btn-link" onclick="UmbClientMgr.closeModalWindow()"><%=umbraco.ui.Text("general", "cancel")%></a>
            <asp:Button ID="btnUpdate" runat="server" CssClass="btn btn-primary" OnClick="btnUpdate_Click"></asp:Button>
        </div>
    </asp:PlaceHolder>
 </asp:Content>