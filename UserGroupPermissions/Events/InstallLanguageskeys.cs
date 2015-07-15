using Umbraco.Core;
using UserGroupPermissions.Businesslogic;

namespace UserGroupPermissions.Events
{
    public class InstallLanguageskeys : ApplicationEventHandler 
    {
        public InstallLanguageskeys()
        {
            Languagefiles.InstallLanguageKey("UserGroupPermissions", "User Group Permissions");
        }
    }
}