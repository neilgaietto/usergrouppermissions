using umbraco.interfaces;

namespace UserGroupPermissions.MenuActions
{
    public class UserGroupPermissions : IAction
    {

        private readonly static UserGroupPermissions _instance = new UserGroupPermissions();

        private UserGroupPermissions() { }

        public static UserGroupPermissions Instance
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
                return "UserGroupPermissions";
            }
        }

        public bool CanBePermissionAssigned
        {
            get
            {
                return true;
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
            get { return 'œ'; }
        }

        public bool ShowInNotifier
        {
            get
            {
                return true;
            }
        }

    }
}