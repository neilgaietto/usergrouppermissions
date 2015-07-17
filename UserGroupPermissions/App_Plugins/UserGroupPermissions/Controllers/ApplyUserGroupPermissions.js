function ApplyUserGroupPermissions($scope, navigationService) {

    // Variables used in the view.
    $scope.selectedUserName = $scope.currentNode.name;

    $scope.performApply = function () {
        //TODO: Send request to server-side controller.
        var id = $scope.currentNode.id;
        var url = "";
        navigationService.hideMenu();
    };

    $scope.cancel = function () {
        navigationService.hideDialog();
    };
}

angular.module("umbraco").controller("UGP.ApplyUserGroupPermissions", ApplyUserGroupPermissions);