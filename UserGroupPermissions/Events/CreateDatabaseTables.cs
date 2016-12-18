namespace UserGroupPermissions.Events
{

    // Namespaces.
    using Models;
    using System;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Persistence;


    /// <summary>
    /// Creates database tables on application startup.
    /// </summary>
    public class CreateDatabaseTables : ApplicationEventHandler
    {

        #region Event Handlers

        /// <summary>
        /// Application started.
        /// </summary>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication,
            ApplicationContext applicationContext)
        {

            // Swallow errors to avoid affecting site if database permissions are insufficient.
            try
            {

                // Variables.
                var dbContext = applicationContext.DatabaseContext;
                var logger = applicationContext.ProfilingLogger.Logger;
                var db = dbContext.Database;
                var dbHelper = new DatabaseSchemaHelper(db, logger, dbContext.SqlSyntax);


                // Create UserTypePermissions table if it doesn't exist yet.
                if (!dbHelper.TableExist("UserTypePermissions"))
                {
                    dbHelper.CreateTable<UserTypePermissionRow>(false);
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