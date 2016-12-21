namespace UserGroupPermissions.Businesslogic
{

    // Namespaces.
    using ExtensionMethods;
    using Models;
    using System;
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
                var userId = user.Id;
                var userTypeId = user.UserType.Id;
                var user2NodeBaseQuery = new Sql()
                    .Select("userId", "nodeId", "permission")
                    .From("umbracoUser2NodePermission")
                    .Where("userId = @0", userId);
                var userTypePermissionsBaseQuery = new Sql()
                    .Select("*")
                    .From("UserTypePermissions")
                    .Where("userTypeId = @0", userTypeId);
                var nodesBaseQuery = new Sql()
                    .Select("id")
                    .From("umbracoNode");
                var user2Node = _sqlHelper.Query<User2NodePermissionDto>(user2NodeBaseQuery);
                var userTypePermissions = _sqlHelper.Query<UserTypePermissionRow>(userTypePermissionsBaseQuery);
                var nodes = _sqlHelper.Query<NodeDto>(nodesBaseQuery);


                // Get the nodes that need permissions assigned.
                var nodesForType = userTypePermissions.Join(nodes,
                    type => type.NodeId,
                    node => node.NodeId,
                    (x, y) => new { type = x, node = y });


                // Get the existing permissions associated with specified nodes.
                var nodesWithPermissions = nodesForType.GroupJoin(user2Node,
                    x => new { x.node.NodeId, Permission = x.type.PermissionId, UserId = userId },
                    x => new { x.NodeId, Permission = x.Permission, x.UserId },
                    (x, y) => new { x.type, x.node, association = y.FirstOrDefault() });


                // Only select the new permissions that haven't been assigned yet.
                var newPermissions = nodesWithPermissions
                    .Where(x => x.type.UserTypeId == userTypeId && x.association == null)
                    .Select(x => new User2NodePermissionDto
                    {
                        UserId = userId,
                        NodeId = x.type.NodeId,
                        Permission = x.type.PermissionId
                    }).ToArray();


                // Insert new permissions.
                _sqlHelper.BulkInsertRecords(newPermissions);

            }
        }


        /// <summary>
        /// Applies all permissions to users othe specified user type.
        /// </summary>
        /// <param name="userType">
        /// Type of the user.
        /// </param>
        /// <param name="node">
        /// The node to apply permissions to.
        /// </param>
        /// <param name="updateDescendants">
        /// Update descendants of the node too?
        /// </param>
        public void ApplyPermissions(IUserType userType, IContent node, bool updateDescendants)
        {
            var nodeIds = GetNodeIdList(node, updateDescendants).ToArray();
            DeleteNodePermissionsForUserType(userType.Id, nodeIds);
            InsertNodePermissionsForUserType(userType.Id, nodeIds);
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

            var items = _sqlHelper.Fetch<User2NodePermissionDto>(
                "select umbracoUser2NodePermission.* from umbracoUser2NodePermission " +
                "join UserTypePermissions on umbracoUser2NodePermission.nodeId = UserTypePermissions.NodeId and umbracoUser2NodePermission.permission = UserTypePermissions.PermissionId " +
                "where umbracoUser2NodePermission.userId = @0 ", userId);

            foreach (var item in items)
            {
                _sqlHelper.Delete<User2NodePermissionDto>(item);
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
                // delete all settings on the node for this user
                DeletePermissions(userType, node);

                // Loop through the permissions and create them
                foreach (char c in permissions)
                {
                    Insert(userType, node, c);
                }

                if (updateChildren)
                {
                    foreach (var childNode in node.Children())
                    {
                        UpdateCruds(userType, childNode, permissions, updateChildren);
                    }
                }
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Returns a node ID list for the specified node.
        /// </summary>
        /// <param name="node">
        /// The node to get the list for.
        /// </param>
        /// <param name="getDescendants">
        /// Gets descendant node ID's?
        /// </param>
        /// <returns>
        /// A list of node ID's.
        /// </returns>
        /// <remarks>
        /// This queries the database for node ID's, which is more performant than using
        /// the content service.
        /// </remarks>
        private List<int> GetNodeIdList(IContent node, bool getDescendants)
        {
            var ids = new List<int>() { node.Id };
            if (getDescendants)
            {
                var pathFilter = node.Path + ",%";
                var query = new Sql().Select("id")
                    .From("umbracoNode")
                    .Where("[path] LIKE @0", pathFilter);
                var descendantIds = _sqlHelper.Query<int>(query);
                ids.AddRange(descendantIds);
            }
            return ids;
        }


        /// <summary>
        /// Deletes permission records from the umbracoUser2NodePermission table for the
        /// specified user type and specified node ID's.
        /// </summary>
        /// <param name="userTypeId">
        /// The user type to delete permissions for.
        /// </param>
        /// <param name="nodeIds">
        /// The node ID's to delete permissions from.
        /// </param>
        private void DeleteNodePermissionsForUserType(int userTypeId, int[] nodeIds)
        {
            var query = @"
                DELETE u2n FROM umbracoUser2NodePermission u2n
                JOIN umbracoUser uu ON u2n.userId = uu.id
                WHERE uu.userType = @0 AND nodeId IN (@1) AND uu.id > 0";
            foreach (var groupedNodeIds in nodeIds.InGroupsOf(1000))
            {
                _sqlHelper.Execute(query, userTypeId, groupedNodeIds);
            }
        }


        /// <summary>
        /// Inserts permission records into the umbracoUser2NodePermission for the
        /// specified user type and specified node ID's.
        /// </summary>
        /// <param name="userTypeId">
        /// The user type to insert permissions for.
        /// </param>
        /// <param name="nodeIds">
        /// The node ID's to insert permissions for.
        /// </param>
        public void InsertNodePermissionsForUserType(int userTypeId, int[] nodeIds)
        {
            foreach (var groupedNodeIds in nodeIds.InGroupsOf(1000))
            {
                var query = new Sql()
                    .Select("c.id userId", "a.nodeId", "a.PermissionId permission")
                    .From("UserTypePermissions a")
                    .InnerJoin("umbracoUser c")
                    .On("a.UserTypeId = c.userType")
                    .LeftOuterJoin("umbracoUser2NodePermission b")
                    .On("a.NodeId = b.nodeId AND a.PermissionId = b.permission AND b.userId = c.id")
                    .Where("a.UserTypeId = @0 AND a.NodeId IN (@1) AND b.userId IS NULL",
                        userTypeId, groupedNodeIds);
                var rows = _sqlHelper.Query<User2NodePermissionDto>(query);
                _sqlHelper.BulkInsertRecords(rows);
            }
        }

        #endregion

    }

}