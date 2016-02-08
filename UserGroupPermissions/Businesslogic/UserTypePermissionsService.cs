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
        /// Returns the permissions for a user
        /// </summary>
        /// <param name="userType"></param>
        /// <returns></returns>
        public IEnumerable<UserTypePermissionRow> GetUserTypePermissions(IUserType userType)
        {
            var items = _sqlHelper.Fetch<UserTypePermissionRow>("select * from UserTypePermissions where UserTypeId = @0 order by NodeId", userType.Id);

            return items;
        }
        /// <summary>
        /// Returns the permissions for a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<UserTypePermissionRow> GetNodeUserTypePermissions(IContent node)
        {
            var items = _sqlHelper.Fetch<UserTypePermissionRow>("select * from UserTypePermissions where NodeId = @0", node.Id);

            return items;
        }

        public List<int> GetNodeIdList(IContent node, bool getChildNodes)
        {
            var ids = new List<int>() { node.Id };
            if (getChildNodes)
            {
                var childNodeIds = _sqlHelper.Fetch<int>("select id from umbracoNode where [path] like '%,' + @0 + ',%' order by [level], id", node.Id.ToString());
                ids.AddRange(childNodeIds);
            }
            return ids;
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
        /// Inserts permissions for all nodes
        /// Does not check for existing permissions
        /// </summary>
        /// <param name="userTypeId"></param>
        /// <param name="iNodeIDs"></param>
        /// <param name="permissionKey"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void InsertUserTypePermissions(int userTypeId, int[] iNodeIDs, char permissionKey)
        {
            foreach (var groupedIds in iNodeIDs.InGroupsOf(2000))
            {
                _sqlHelper.Execute("INSERT INTO [UserTypePermissions] ([NodeId],[UserTypeId],[PermissionId]) " +
                                   "select id, @0, @1 from umbracoNode where id IN (@2) ", userTypeId, permissionKey, groupedIds);
            }
        }


        /// <summary>
        /// Copies the user type permissions for single user.
        /// </summary>
        /// <param name="user">The user.</param>
        public void CopyPermissionsForSingleUser(IUser user)
        {
            if (!user.IsAdmin() && !user.Disabled())
            {
                // Copy usertype permissions to user permissions table if the permission doesnt already exist
                var userTypeId = user.UserType.Id;
                var userId = user.Id;
                _sqlHelper.Execute("INSERT INTO [umbracoUser2NodePermission] ([userId], [nodeId], [permission]) " +
                                    "SELECT @0, a.[NodeId], a.[PermissionId] FROM [UserTypePermissions] a " +
                                    "LEFT OUTER JOIN[umbracoUser2NodePermission] b on a.NodeId = b.nodeId and a.PermissionId = b.permission and b.userId = @0 " +
                                    "WHERE a.[UserTypeId] = @1 AND B.userId is null ", userId, userTypeId);
            }
        }


        /// <summary>
        /// Copies Node Permissions
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CopyNodeUserTypePermissions(IContent sourceNode, IContent targetNode)
        {
            _sqlHelper.Execute("INSERT INTO [UserTypePermissions] ([NodeID], [UserTypeId], [PermissionId]) " +
                                "SELECT @1, [UserTypeId], [PermissionId] FROM [UserTypePermissions] WHERE NodeId = @0", sourceNode.Id, targetNode.Id);
        }

        /// <summary>
        /// Copies all permissions to related users of the user type.
        /// </summary>
        /// <param name="userType">Type of the user.</param>
        /// <param name="node">The node.</param>
        /// <param name="updateChildren"></param>
        public void ApplyPermissions(IUserType userType, IContent node, bool updateChildren)
        {
            var nodeIds = GetNodeIdList(node, updateChildren).ToArray();
            DeletePermissions(userType.Id, nodeIds);
            InsertPermissions(userType.Id, nodeIds);
        }

        public void DeletePermissions(int userTypeId, int[] nodeIds)
        {
            foreach (var groupedNodeIds in nodeIds.InGroupsOf(1000))
            {
                _sqlHelper.Execute("DELETE a from [umbracoUser2NodePermission] a " +
                                    "join [umbracoUser] b on a.userId = b.id " +
                                    "where b.userType = @0 and nodeId in (@1) and b.id > 0 ", userTypeId, groupedNodeIds);
            }
        }

        public void InsertPermissions(int userTypeId, int[] nodeIds)
        {
            foreach (var groupedNodeIds in nodeIds.InGroupsOf(1000))
            {
                _sqlHelper.Execute(@"INSERT INTO [umbracoUser2NodePermission] ([userId], [nodeId], [permission]) " +
                                    "SELECT c.id, a.[NodeId], a.[PermissionId] " +
                                    "FROM [UserTypePermissions] a  " +
                                    "JOIN [umbracoUser] c on a.UserTypeId = c.userType " +
                                    "LEFT OUTER JOIN [umbracoUser2NodePermission] b on a.NodeId = b.nodeId and a.PermissionId = b.permission and b.userId = c.id " +
                                    "WHERE a.[UserTypeId] = @0 AND a.[NodeId] in (@1) AND B.userId is null ", userTypeId, groupedNodeIds);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateUserTypePermissions(IUserType userType, IContent node, IEnumerable<char> permissions, bool updateChildren)
        {
            // do not act on admin user types.
            if (!userType.IsAdmin())
            {
                var nodeIds = GetNodeIdList(node, updateChildren).ToArray();
                // delete all settings on the node for this user
                DeleteUserTypePermissions(userType.Id, nodeIds);

                // Loop through the permissions and create them
                foreach (char c in permissions)
                {
                    InsertUserTypePermissions(userType.Id, nodeIds, c);
                }
            }
        }

        /// <summary>
        /// deletes all permissions for the user Type
        /// </summary>
        /// <param name="userType"></param>
        public void DeleteUserTypePermissions(IUserType userType)
        {
            // delete all settings on the node for this user
            _sqlHelper.Execute("delete from UserTypePermissions where UserTypeId=@0 ", userType.Id);

        }

        /// <summary>
        /// delete all permissions for this node
        /// </summary>
        /// <param name="node"></param>
        public void DeleteUserTypePermissions(IContent node)
        {
            _sqlHelper.Execute("delete from UserTypePermissions where NodeId = @0", node.Id);
        }

        /// <summary>
        /// Delets all permissions for the node/user combination
        /// </summary>
        public void DeleteUserTypePermissions(int userTypeId, int[] iNodeIDs)
        {
            foreach (var groupedIds in iNodeIDs.InGroupsOf(2000))
            {
                _sqlHelper.Execute("delete from UserTypePermissions where NodeId IN (@0) AND UserTypeId=@1 ", groupedIds, userTypeId);
            }

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

        #endregion

    }

}