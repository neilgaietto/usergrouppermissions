using Umbraco.Core;
using UserGroupPermissions.Businesslogic;

namespace UserGroupPermissions.Events
{
    public class InstallLanguagesKeys : ApplicationEventHandler 
    {
        public InstallLanguagesKeys()
        {
            LanguageFiles.InstallLanguageKey("UserGroupPermissions", "User Group Permissions");
        }
    }
}