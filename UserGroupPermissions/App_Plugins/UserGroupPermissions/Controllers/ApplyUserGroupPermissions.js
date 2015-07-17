function ApplyUserGroupPermissions($scope, navigationService, $http) {

    // Variables used in the view.
    $scope.selectedUserName = $scope.currentNode.name;

    // Apply the group permissions.
    $scope.performApply = function () {
        var id = parseInt($scope.currentNode.id);
        var url = "/umbraco/backoffice/UGP/UserGroupPermissions/ApplyAllGroupPermissions";
        var data = { "UserId": id };
        var strData = JSON.stringify(data);
        var options = {
            headers: {
                "Content-Type": "application/json"
            }
        };
        $http.post(url, strData, options).success(function () {
            //TODO: ...
            navigationService.hideMenu();
        })
        .error(function () {
            //TODO: Failure.
            alert("Error with api post.");
        });
    };

    // Close dialog.
    $scope.cancel = function () {
        navigationService.hideDialog();
    };

}

angular.module("umbraco").controller("UGP.ApplyUserGroupPermissions", ApplyUserGroupPermissions);