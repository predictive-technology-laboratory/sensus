'use strict';

angular.module('myApp.CreateStudyPage', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
  $routeProvider.when('/CreateStudyPage', {
    templateUrl: 'Views/CreateStudyPage.html',
    controller: 'CreateStudyPageCtrl'
  });
}])

.controller('CreateStudyPageCtrl', function($scope, $http, $location) {
	$scope.formData = {};
	
	$scope.onCreate = function() {
		var create;
		$http({
  			method  : 'POST',
  			url     : 'http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/create_study.php',
  			data    : $.param($scope.formData),
  			headers	: { 'Content-type': 'application/x-www-form-urlencoded' },
 		})
  		.success(function(data) {
      			create = true;
			$location.path('/StudyLandingPage');
    		});
		$http({
			method	: 'POST',
			url	: 'http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/update_user_status.php',
			data    : $.param($scope.formData),
  			headers	: { 'Content-type': 'application/x-www-form-urlencoded' },
		}).success(function(data) {
			if (create)
				$location.path('/StudyLandingPage');
		});
  	};
  	
  	$scope.onCancel = function() {
    		$location.path('/StudyLandingPage');
  	};
});
