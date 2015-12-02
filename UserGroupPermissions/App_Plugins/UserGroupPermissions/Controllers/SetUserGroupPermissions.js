function SetUserGroupPermissions($scope, navigationService, notificationsService, $http) {

    // Variables used in the view.
    $scope.pageName = $scope.currentNode.name;
    $scope.userOptions = {
        applyPermissions: false
    };
    $scope.processingMessage = "Processing request. Please wait.";
    $scope.successMessage = "Operation successful.";
    $scope.failureMessage = "An unknown error occurred.";
    $scope.showSelection = false;
    $scope.showConfirmButtons = false;
    $scope.showSuccess = false;
    $scope.showFailure = false;
    $scope.showDoneButton = false;
    $scope.showProgress = true;
    $scope.applyPermissionsId = "ugp-apply-permissions-" + Math.random().toString().replace(".", "");
    $scope.userTypes = [];
    $scope.permissions = [];

    // Gets permissions from the server.
    function getPermissions() {

        // Variables.
        var id = parseInt($scope.currentNode.id);
        var url = "/umbraco/backoffice/UGP/UserGroupPermissions/GetGroupPermissions";
        var options = {
            cache: false,
            params: {
                "NodeId": id,
                // Cache buster ensures requests aren't cached.
                "CacheBuster": Math.random()
            }
        };

        // Request user type permissions from server.
        $http.get(url, options).success(function (data) {

            // Set scope variables based on permissions.
            $scope.userTypes = data.UserTypePermissions;
            for (var i = 0; i < data.UserTypePermissions[0].Permissions.length; i++) {
                var permission = data.UserTypePermissions[0].Permissions[i];
                var userTypes = [];
                for (var j = 0; j < data.UserTypePermissions.length; j++) {
                    var userType = data.UserTypePermissions[j];
                    userTypes.push({
                        UserTypeId: userType.UserTypeId,
                        HasPermission: userType.Permissions[i].HasPermission
                    });
                }
                $scope.permissions.push({
                    Label: permission.Label,
                    Letter: permission.Letter,
                    UserTypes: userTypes
                });
            }

            // Update UI.
            $scope.showProgress = false;
            $scope.showSelection = true;
            $scope.showConfirmButtons = true;

        });
    }
    getPermissions();

    // Update the group permissions.
    $scope.updatePermissions = function () {

        // Store permissions by user type ID.
        var userTypePermissions = {};
        for (var i = 0; i < $scope.permissions.length; i++) {
            var permission = $scope.permissions[i];
            for (var j = 0; j < permission.UserTypes.length; j++) {
                var userType = permission.UserTypes[j];
                var existingUserType = userTypePermissions[userType.UserTypeId];
                existingUserType = existingUserType || {
                    UserTypeId: userType.UserTypeId,
                    Permissions: []
                };
                userTypePermissions[userType.UserTypeId] = existingUserType;
                if (userType.HasPermission) {
                    existingUserType.Permissions.push(permission.Letter);
                }
            }
        }

        // Convert permissions to format expected by server.
        var dataPermissions = [];
        for (var i = 0; i < $scope.permissions[0].UserTypes.length; i++) {
            var userTypeId = $scope.permissions[0].UserTypes[i].UserTypeId;
            var permission = userTypePermissions[userTypeId];
            if (permission) {
                dataPermissions.push(userTypePermissions[userTypeId]);
            }
        }

        // Variables.
        var id = parseInt($scope.currentNode.id);
        var url = "/umbraco/backoffice/UGP/UserGroupPermissions/SetGroupPermissions";
        var data = {
            "NodeId": id,
            "ReplacePermissionsOnUsers": $scope.userOptions.applyPermissions,
            "UserTypePermissions": dataPermissions
        };
        var strData = JSON.stringify(data);
        var options = {
            headers: {
                "Content-Type": "application/json"
            }
        };

        // Send request to update permissions.
        $scope.showProgress = true;
        $scope.showSelection = false;
        $scope.showConfirmButtons = false;
        $http.post(url, strData, options).success(function (data) {

            // Was the request successful?
            if (data.Success) {
                $scope.successMessage = "The permissions were successfully updated.";
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

angular.module("umbraco").controller("UGP.SetUserGroupPermissions", SetUserGroupPermissions);