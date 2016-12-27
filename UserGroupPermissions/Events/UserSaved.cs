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
    public class UserSaved : ApplicationEventHandler
    {

        #region Readonly Variables

        private readonly UserTypePermissionsService _userTypePermissionsService;

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public UserSaved()
        {
            _userTypePermissionsService = new UserTypePermissionsService();
            UserService.SavingUser += User_Saving;
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// User saving event.
        /// </summary>
        /// <remarks>
        /// User is already saved but the object itself is not updated yet.
        /// </remarks>
        void User_Saving(IUserService service, SaveEventArgs<IUser> e)
        {
            foreach (var savedEntity in e.SavedEntities)
            {
                if (savedEntity != null)
                {
                    if (!savedEntity.IsNewEntity())
                    {

                        // Update permissions for old users if the user type changes.
                        IUser savedUser = service.GetUserById(savedEntity.Id);
                        if (savedEntity.UserType.Alias != savedUser.UserType.Alias)
                        {
                            //clear previous role permissions
                            _userTypePermissionsService.DeleteNodePermissionsForUser(savedEntity.Id);

                            //set new role permissions
                            _userTypePermissionsService.CopyPermissionsForSingleUser(savedEntity);
                        }

                    }
                    else
                    {

                        // Set permissions for new users.
                        _userTypePermissionsService.CopyPermissionsForSingleUser(savedEntity);

                    }
                }
            }
        }

        #endregion

    }

}