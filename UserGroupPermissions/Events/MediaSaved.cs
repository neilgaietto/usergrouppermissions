using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web;
using UserGroupPermissions.ExtensionMethods;

namespace UserGroupPermissions.Events
{
    public class MediaSaved : ApplicationEventHandler
    {
        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MediaSaved()
        {
            MediaService.Saving += MediaService_Saving;
        }

        private void MediaService_Saving(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            //checks if the current users role has media upload disabled
            var currentUser = UmbracoContext.Current.Security.CurrentUser;
            if (currentUser.UserType.HasDisabledMediaUpload())
            {
                e.CancelOperation(new EventMessage("Media", "Invalid permissions to upload media."));
            }
        }

        #endregion


        #region Event Handlers
        #endregion
    }
}