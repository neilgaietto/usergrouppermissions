namespace UserGroupPermissions.Businesslogic
{

    // Namespaces.
    using ExtensionMethods;
    using Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.Membership;
    using Umbraco.Core.Persistence;


    /// <summary>
    /// Service to help with user permissions.
    /// </summary>
    public class UserTypePermissionsService
    {

        #region Readonly Variables

        private readonly Database _sqlHelper;

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public UserTypePermissionsService()
        {
            _sqlHelper = ApplicationContext.Current.DatabaseContext.Database;
        }

        #endregion


        #region Public Methods

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Insert(IUserType userType, IContent node, char permissionKey)
        {

            // Method is synchronized so exists remains consistent (avoiding race condition)

            bool exists = _sqlHelper.Fetch<int>("SELECT UserTypeId FROM UserTypePermissions WHERE UserTypeId = @0 AND NodeId = @1 AND PermissionId = @2", userType.Id, node.Id, permissionKey.ToString()).Any();
            if (!exists)
            {
                var newPerms = new UserTypePermissionRow
                {
                    NodeId = node.Id,
                    PermissionId = permissionKey.ToString(),
                    UserTypeId = userType.Id,
                };

                _sqlHelper.Insert(newPerms);

            }
                

        }


        /// <summary>
        /// Returns the permissions for a user
        /// </summary>
        /// <param name="userType"></param>
        /// <returns></returns>
        public IEnumerable<UserTypePermissionRow> GetUserTypePermissions(IUserType userType)
        {

            var items = _sqlHelper.Fetch<UserTypePermissionRow>(
                "select * from UserTypePermissions  where UserTypeId = @0 order by NodeId", userType.Id);

            return items;
        }


        public string GetPermissions(IUserType userType, string path)
        {

            string defaultPermissions = String.Join(string.Empty, userType.Permissions);
            
            var allUserPermissions = GetUserTypePermissions(userType).GroupBy(x => x.NodeId).ToList();

            foreach (string nodeId in path.Split(','))
            {
                var parsedNodeId = int.Parse(nodeId);
                if (allUserPermissions.Select(x => x.Key).Contains(parsedNodeId))
                {
                    var userTypenodePermissions =  String.Join(string.Empty,
                        allUserPermissions.FirstOrDefault(x => x.Key == parsedNodeId)
                        .Select(x => x.PermissionId));

                    if (!string.IsNullOrEmpty(userTypenodePermissions))
                    {
                        defaultPermissions = userTypenodePermissions;
                    }
                }
            }

            return defaultPermissions;
        }


        /// <summary>
        /// Returns the permissions for a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<UserTypePermissionRow> GetNodePermissions(IContent node)
        {
            var items = _sqlHelper.Fetch<UserTypePermissionRow>(
                "select * from UserTypePermissions where NodeId = @0 order by nodeId", node.Id);

            return items;
        }


        /// <summary>
        /// Copies the user type permissions for single user.
        /// </summary>
        /// <param name="user">The user.</param>
        public void CopyPermissionsForSingleUser(IUser user)
        {
            if (!user.IsAdmin() && !user.Disabled())
            {

                // Variables.
                var permissions = GetUserTypePermissions(user.UserType);
                var contentService = ApplicationContext.Current.Services.ContentService;
                var nodesById = new Dictionary<int, IContent>();
                var node = default(IContent);
                var userId = new [] { user.Id };


                // Apply each permission.
                foreach (var permission in permissions)
                {

                    // Get node (try cache first).
                    if (!nodesById.TryGetValue(permission.NodeId, out node))
                    {
                        node = contentService.GetById(permission.NodeId);
                        nodesById[permission.NodeId] = node;
                    }


                    // Apply permission to node.
                    if (!string.IsNullOrWhiteSpace(permission.PermissionId) && node != null)
                    {
                        var permissionId = permission.PermissionId[0];
                        contentService.AssignContentPermission(node, permissionId, userId);
                    }

                }

            }
        }


        /// <summary>
        /// Copies all permissions to related users of the user type.
        /// </summary>
        /// <param name="userType">Type of the user.</param>
        /// <param name="node">The node.</param>
        public void CopyPermissions(IUserType userType, IContent node)
        {
            IEnumerable<char>permissions = GetPermissions(userType, node.Path);

            foreach (IUser user in userType.GetAllRelatedUsers())
            {
                if (!user.IsAdmin() && !user.Disabled())
                {
                    ApplicationContext.Current.Services.UserService
                        .ReplaceUserPermissions(user.Id, permissions, node.Id);
                }
            }
        }


        /// <summary>
        /// Delets all permissions for the node/user combination
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userType"></param>
        /// <param name="node"></param>
        public void DeletePermissions(IUserType userType, IContent node)
        {
            // delete all settings on the node for this user

            _sqlHelper.Execute("delete from UserTypePermissions where UserTypeId=@0 and NodeId = @1", userType.Id, node.Id);

        }


        /// <summary>
        /// deletes all permissions for the user
        /// </summary>
        /// <param name="userType"></param>
        public void DeletePermissions(IUserType userType)
        {
            // delete all settings on the node for this user

            _sqlHelper.Execute("delete from UserTypePermissions where UserTypeId=@0 ", userType.Id);

        }


        public void DeletePermissions(int userTypeId, int[] iNodeIDs)
        {
                
            string nodeIDs = string.Join(",", Array.ConvertAll<int, string>(iNodeIDs, Converter));

            _sqlHelper.Execute("delete from UserTypePermissions where NodeId IN (@0) AND UserTypeId=@1 ", nodeIDs, userTypeId);

        }


        private string Converter(int from)
        {
            return from.ToString();
        }


        /// <summary>
        /// delete all permissions for this node
        /// </summary>
        /// <param name="node"></param>
        public void DeletePermissions(IContent node)
        {
            _sqlHelper.Execute("delete from UserTypePermissions where NodeId = @0", node.Id);
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateCruds(IUserType userType, IContent node, string permissions)
        {
            // do not act on admin user types.
            if (userType.Alias != "admin")
            {
                // delete all settings on the node for this user
                DeletePermissions(userType, node);

                // Loop through the permissions and create them
                foreach (char c in permissions.ToCharArray())
                    Insert(userType, node, c);
            }
        }

        #endregion

    }

}