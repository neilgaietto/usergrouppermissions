

--select usertypes to copy
DECLARE @sourceId int = 16
DECLARE @targetId int = 15

--copy user type permissions
INSERT INTO [UserTypePermissions] ([NodeId], [UserTypeId], [PermissionId])
SELECT a.[NodeId],@targetId,a.[PermissionId]
  FROM [dbo].[UserTypePermissions] a
  LEFT OUTER JOIN [dbo].[UserTypePermissions] b on a.NodeId = b.NodeId and a.PermissionId = b.PermissionId  and b.UserTypeId = @targetId
  where a.[UserTypeId] = @sourceId
  AND b.AutoId is null


--apply missing node permissions

INSERT INTO [umbracoUser2NodePermission] ([userId], [nodeId], [permission]) 
SELECT c.id, a.[NodeId], a.[PermissionId] 
FROM [UserTypePermissions] a  
JOIN [umbracoUser] c on a.UserTypeId = c.userType 
LEFT OUTER JOIN [umbracoUser2NodePermission] b on a.NodeId = b.nodeId and a.PermissionId = b.permission and b.userId = c.id 
WHERE a.[UserTypeId] = @targetId  AND B.userId is null 