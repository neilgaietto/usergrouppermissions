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
        /// Copies all permissions to related users of the user type.
        /// </summary>
        /// <param name="userType">Type of the user.</param>
        /// <param name="node">The node.</param>
        /// <param name="updateChildren">
        /// Update child nodes too?
        /// </param>
        public void CopyPermissions(IUserType userType, IContent node, bool updateChildren)
        {

            // Variables.
            var permissions = GetPermissions(userType, node.Path);
            var userService = ApplicationContext.Current.Services.UserService;


            // Set permissions for each user.
            foreach (IUser user in userType.GetAllRelatedUsers())
            {
                if (!user.IsAdmin() && !user.Disabled())
                {
                    userService.ReplaceUserPermissions(user.Id, permissions, node.Id);
                }
            }


            // Apply permissions to descendants as well?
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

    }

}