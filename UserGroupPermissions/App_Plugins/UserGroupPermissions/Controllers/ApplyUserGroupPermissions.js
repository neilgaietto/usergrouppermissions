function ApplyUserGroupPermissions($scope, navigationService, notificationsService, $http) {

    // Variables used in the view.
    $scope.selectedUserName = $scope.currentNode.name;
    $scope.showSuccess = false;
    $scope.successMessage = "Operation successful.";
    $scope.showFailure = false;
    $scope.failureMessage = "An unknown error occurred.";
    $scope.showConfirmButtons = true;
    $scope.showDoneButton = false;
    $scope.showQuestion = true;
    $scope.showProgress = false;

    // Apply the group permissions.
    $scope.performApply = function () {

        // Variables.
        var id = parseInt($scope.currentNode.id);
        var url = "/umbraco/backoffice/UGP/UserGroupPermissions/ApplyAllGroupPermissions";
        var data = { "UserId": id };
        var strData = JSON.stringify(data);
        var options = {
            headers: {
                "Content-Type": "application/json"
            }
        };

        // Send request to apply permissions.
        $scope.showProgress = true;
        $scope.showQuestion = false;
        $scope.showConfirmButtons = false;
        $http.post(url, strData, options).success(function (data) {

            // Was the request successful?
            if (data.Success) {
                $scope.successMessage = "The permissions were successfully applied.";
                $scope.showSuccess = true;
            } else {
                $scope.failureMessage = data.Reason;
                $scope.showFailure = true;
                notificationsService.error("Unexpected Error", data.Reason);
            }
            $scope.showProgress = false;
            $scope.showDoneButton = true;

        })
        .error(function () {

            // An error occurred.
            $scope.showProgress = false;
            $scope.showFailure = true;
            $scope.showDoneButton = true;
            $scope.failureMessage = "There was an issue communicating with the server.";
            notificationsService.error("Server Error", $scope.failureMessage);

        });

    };

    // Cancel.
    $scope.cancel = function () {
        navigationService.hideDialog();
    };

    // Close dialog.
    $scope.closeDialog = function () {
        navigationService.hideDialog();
    };

}

angular.module("umbraco").controller("UGP.ApplyUserGroupPermissions", ApplyUserGroupPermissions);