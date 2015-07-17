using Umbraco.Core;
using UserGroupPermissions.Businesslogic;

namespace UserGroupPermissions.Events
{
    public class InstallLanguagesKeys : ApplicationEventHandler 
    {
        public InstallLanguagesKeys()
        {
            Languagefiles.InstallLanguageKey("UserGroupPermissions", "User Group Permissions");
        }
    }
}