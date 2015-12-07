using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.interfaces;

namespace UserGroupPermissions.MenuActions
{
    public class DisableMediaUploadPermissions : IAction
    {

        private readonly static DisableMediaUploadPermissions _instance = new DisableMediaUploadPermissions();

        private DisableMediaUploadPermissions() { }

        public static DisableMediaUploadPermissions Instance
        {
            get
            {
                return _instance;
            }
        }

        public string Alias
        {
            get
            {
                return "DisableMediaUploadPermissions";
            }
        }

        public bool CanBePermissionAssigned
        {
            get
            {
                return false;
            }
        }

        public string Icon
        {
            get
            {
                return string.Empty;
            }
        }

        public string JsFunctionName
        {
            get
            {
                return string.Empty;

            }
        }

        public string JsSource
        {
            get
            {
                return string.Empty;
            }
        }

        public char Letter
        {
            get { return 'ß'; }
        }

        public bool ShowInNotifier
        {
            get
            {
                return false;
            }
        }

    }
}