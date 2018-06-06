'use strict';

angular.module('myApp.LoginPage', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
  $routeProvider.when('/LoginPage', {
    templateUrl: 'Views/LoginPage.html',
    controller: 'LoginPageCtrl'
  });
}])

.controller('LoginPageCtrl', function($scope, $http, $location, $route) {
	$scope.formData = {};
	
	$scope.onLogin = function() {
                // TODO check login credentials against database using http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/check_login_credentials.php
                $http({
                        method  : 'POST',
                        url     : 'http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/check_login_credentials.php',
                        data    : $.param($scope.formData),
                        headers : { 'Content-type': 'application/x-www-form-urlencoded' },
                }).success(function(data) {
			if (data != 'null') {
           			$location.path('/StudyLandingPage');
           		} else {
				alert("Bad email address or password");
			}                	
                });
		//$http({
                //        method  : 'POST',
                //        url     : 'http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/update_user_status.php',
                //        data    : $.param($scope.formData),
                //        headers : { 'Content-type': 'application/x-www-form-urlencoded' },
                //}).success(function(data) {
                //	  $location.path('/StudyLandingPage');
                //});
        };

	$scope.onCreate = function() {
		$location.path('/CreateResearcherPage');
	}
});
