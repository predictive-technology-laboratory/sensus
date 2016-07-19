'use strict';

angular.module('myApp.StudyLandingPage', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
  $routeProvider.when('/StudyLandingPage', {
    templateUrl: 'Views/StudyLandingPage.html',
    controller: 'StudyLandingPageCtrl'
  });
}])

.controller('StudyLandingPageCtrl', function($scope, $http, $location, $route) {
	$scope.studies = [];
	$scope.message = '';

	$(document).ready(function() {
		$http({
			method : 'GET',
			url : 'http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/load_studies.php',
			dataType : "json",
			context : document.body
		}).success(function(data) {
			if (data.indexOf('br') > -1) {
				$scope.message = 'No studies found.';
			}
			else if (data != 'null') {
				for (var i = 0; i < data.length; i += 1) {
					$scope.studies.push({
						name : data[i].name
					})
				}
			}
		});
	});

	$scope.onSelect = function(study) {
		$scope.selectOrDeleteStudy(study, false);
	};

	$scope.onDelete = function(study) {
		$scope.selectOrDeleteStudy(study, true);
	};

	$scope.onCreate = function() {
		$location.path('/CreateStudyPage');
	};

	$scope.selectOrDeleteStudy = function(study, isDelete) {
		var data = '';
		data += 'studyName=' + study.name;
		$http({
			method	: 'POST',
			url : isDelete ? 'http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/delete_study.php' : 'http://ec2-107-22-158-28.compute-1.amazonaws.com/ajax/update_user_status.php',
			data    : data,
 			headers	: { 'Content-type': 'application/x-www-form-urlencoded' },
		}).success(function(data) {
			if (isDelete) {
				$route.reload();
			} else {
				$location.path('/AnotherPage');
			}
		});
	};
});
