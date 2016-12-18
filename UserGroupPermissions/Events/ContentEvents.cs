namespace UserGroupPermissions.Events
{

    // Namespaces.
    using Businesslogic;
    using Umbraco.Core;
    using Umbraco.Core.Events;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;


    /// <summary>
    /// Events related to Umbraco content.
    /// </summary>
    public class ContentEvents : ApplicationEventHandler
    {

        #region Readonly Variables

        private readonly UserTypePermissionsService userTypePermissionsService;

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ContentEvents()
        {
            userTypePermissionsService = new UserTypePermissionsService();
            ContentService.Created += ContentService_Created;
            ContentService.Deleted += ContentService_Deleted;
            ContentService.Saved += ContentService_Saved;
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// Content saved.
        /// </summary>
        private void ContentService_Saved(IContentService sender, SaveEventArgs<IContent> e)
        {

            // Copy parent permissions to new content node.
            foreach (var entity in e.SavedEntities)
            {
                var parent = entity.Parent();
                if (!entity.IsNewEntity() || parent == null)
                {
                    return;
                }
                userTypePermissionsService.CopyNodePermissions(parent, entity);
            }

        }


        /// <summary>
        /// Content created.
        /// </summary>
        private void ContentService_Created(IContentService sender, NewEventArgs<IContent> e)
        {

            // Copy parent permissions to new content node.
            if (e.Parent == null || e.Entity == null || !e.Entity.IsNewEntity())
            {
                return;
            }
            userTypePermissionsService.CopyNodePermissions(e.Parent, e.Entity);

        }


        /// <summary>
        /// Content deleted.
        /// </summary>
        private void ContentService_Deleted(IContentService sender, DeleteEventArgs<IContent> e)
        {

            // Clear permissions for removed content.
            foreach (var deletedNode in e.DeletedEntities)
            {
                userTypePermissionsService.DeletePermissions(deletedNode);
            }

        }

        #endregion

    }

}