using System.Linq;
using UserGroupPermissions.ExtensionMethods;
using UserGroupPermissions.MenuActions;

namespace UserGroupPermissions.Events
{

    // Namespaces.
    using Businesslogic;
    using Umbraco.Core;
    using Umbraco.Core.Events;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.Membership;
    using Umbraco.Core.Services;


    /// <summary>
    /// Handles user saved event.
    /// </summary>
    public class UserTypeSaved : ApplicationEventHandler
    {

        

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public UserTypeSaved()
        {

            UserService.SavingUserType += UserService_SavingUserType;
        }



        #endregion


        #region Event Handlers

        
        private void UserService_SavingUserType(IUserService sender, SaveEventArgs<IUserType> e)
        {
            
            foreach (var role in e.SavedEntities)
            {
                var existing = ApplicationContext.Current.Services.UserService.GetUserTypeById(role.Id);

                //preserves the disabled media permission
                if (existing.HasDisabledMediaUpload() && !role.HasDisabledMediaUpload())
                {
                    role.Permissions = role.Permissions.Union(new[] { DisableMediaUploadPermissions.Instance.Letter.ToString() });
                }
            }

        }

        #endregion

    }

}