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

        /// <summary>
        /// Inserts permissions for all nodes
        /// Does not check for existing permissions
        /// </summary>
        /// <param name="userTypeId"></param>
        /// <param name="iNodeIDs"></param>
        /// <param name="permissionKey"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Insert(int userTypeId, int[] iNodeIDs, char permissionKey)
        {
            foreach (var groupedIds in iNodeIDs.InGroupsOf(2000))
            {
                string nodeIDs = string.Join(",", groupedIds);
                _sqlHelper.Execute("INSERT INTO [UserTypePermissions] ([NodeId],[UserTypeId],[PermissionId]) select id, @1, '@2' from umbracoNode where id IN (@0) ", nodeIDs, userTypeId, permissionKey);
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
                    var userTypenodePermissions = String.Join(string.Empty,
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
                var userId = new[] { user.Id };


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
        /// <param name="updateChildren"></param>
        public void CopyPermissions(IUserType userType, IContent node, bool updateChildren)
        {
            IEnumerable<char> permissions = GetPermissions(userType, node.Path);

            foreach (IUser user in userType.GetAllRelatedUsers())
            {
                if (!user.IsAdmin() && !user.Disabled())
                {
                    ApplicationContext.Current.Services.UserService
                        .ReplaceUserPermissions(user.Id, permissions, node.Id);
                }
            }

            if (updateChildren)
            {
                foreach (var childNode in node.Children())
                {
                    CopyPermissions(userType, childNode, updateChildren);
                }
            }
        }


        /// <summary>
        /// Copies Node Permissions
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CopyNodePermissions(IContent sourceNode, IContent targetNode)
        {
            var sourcePermissions = GetNodePermissions(sourceNode);
            foreach (var permission in sourcePermissions)
            {
                permission.NodeId = targetNode.Id;
                _sqlHelper.Insert(permission);
            }
        }


        /// <summary>
        /// deletes all permissions for the user Type
        /// </summary>
        /// <param name="userType"></param>
        public void DeletePermissions(IUserType userType)
        {
            // delete all settings on the node for this user

            _sqlHelper.Execute("delete from UserTypePermissions where UserTypeId=@0 ", userType.Id);

        }



        /// <summary>
        /// deletes all the users inherited permissions for all roles
        /// </summary>
        public void DeleteUsersRolePermissions(int userId)
        {
            // delete all settings on the node for this user
            //TODO: Update to work with SQL CE
            _sqlHelper.Execute("delete u " +
                                "from umbracoUser2NodePermission u " +
                                "join UserTypePermissions p on u.nodeId = p.NodeId and u.permission = p.PermissionId " +
                                "and u.userId = @0 ", userId);

        }

        /// <summary>
        /// Delets all permissions for the node/user combination
        /// </summary>
        public void DeletePermissions(int userTypeId, int[] iNodeIDs)
        {

            foreach (var groupedIds in iNodeIDs.InGroupsOf(2000))
            {
                string nodeIDs = string.Join(",", groupedIds);
                _sqlHelper.Execute("delete from UserTypePermissions where NodeId IN (@0) AND UserTypeId=@1 ", nodeIDs, userTypeId);
            }

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
        public void UpdateCruds(IUserType userType, IContent node, IEnumerable<char> permissions, bool updateChildren)
        {
            // do not act on admin user types.
            if (!userType.IsAdmin())
            {
                var nodeIds = GetNodeIdList(node, updateChildren).ToArray();
                // delete all settings on the node for this user
                DeletePermissions(userType.Id, nodeIds);

                // Loop through the permissions and create them
                foreach (char c in permissions)
                {
                    Insert(userType.Id, nodeIds, c);
                }
            }
        }

        #endregion

        public List<int> GetNodeIdList(IContent node, bool getChildNodes)
        {
            var ids = new List<int>() { node.Id };
            if (getChildNodes)
            {
                var childNodeIds = _sqlHelper.Fetch<int>("select id from umbracoNode where [path] like '%,@0,%' order by [level], id", node.Id);
                ids.AddRange(childNodeIds);
            }
            return ids;
        }

    }

}