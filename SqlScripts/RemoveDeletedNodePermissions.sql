delete from [UserTypePermissions]
where NodeId not in (select id from umbracoNode)