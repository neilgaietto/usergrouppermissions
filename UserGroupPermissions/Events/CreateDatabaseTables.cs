namespace UserGroupPermissions.Events
{

    // Namespaces.
    using System;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Persistence;
    using UserGroupPermissions.Models;


    /// <summary>
    /// Creates database tables on application startup.
    /// </summary>
    public class CreateDatabaseTables : ApplicationEventHandler
    {

        #region Event Handlers

        /// <summary>
        /// Application started.
        /// </summary>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {

            // Swallow errors to avoid affecting site if database permissions are insufficient.
            try
            {

                // Create UserTypePermissions table if it doesn't exist yet.
                var db = applicationContext.DatabaseContext.Database;
                if (!db.TableExist("UserTypePermissions"))
                {
                    db.CreateTable<UserTypePermissionRow>(false);
                }

            }
            catch (Exception ex)
            {
                LogHelper.Error<CreateDatabaseTables>("Unable to create table UserTypePermissions.", ex);
            }

        }

        #endregion

    }

}