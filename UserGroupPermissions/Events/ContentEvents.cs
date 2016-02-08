using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using UserGroupPermissions.Businesslogic;

namespace UserGroupPermissions.Events
{
    public class ContentEvents : ApplicationEventHandler
    {
        #region Readonly Variables

        private readonly UserTypePermissionsService _userTypePermissionsService;

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ContentEvents()
        {
            _userTypePermissionsService = new UserTypePermissionsService();
            ContentService.Created += ContentService_Created;
            ContentService.Deleted += ContentService_Deleted;
            ContentService.Saved += ContentService_Saved;
        }

        private void ContentService_Saved(IContentService sender, SaveEventArgs<IContent> e)
        {
            foreach (var entity in e.SavedEntities)
            {
                if (!entity.IsNewEntity() || entity.Parent() == null) return;
                //copy parent permissions to new content node
                _userTypePermissionsService.CopyNodeUserTypePermissions(entity.Parent(), entity);
            }
        }

        private void ContentService_Created(IContentService sender, NewEventArgs<IContent> e)
        {
            if (e.Parent == null || e.Entity == null || !e.Entity.IsNewEntity()) return;
            //copy parent permissions to new content node
            _userTypePermissionsService.CopyNodeUserTypePermissions(e.Parent, e.Entity);
        }

        private void ContentService_Deleted(IContentService sender, DeleteEventArgs<IContent> e)
        {
            //clear permissions for removed content
            foreach (var deletedNode in e.DeletedEntities)
            {
                _userTypePermissionsService.DeletePermissions(deletedNode);
            }

        }
        #endregion
    }
}