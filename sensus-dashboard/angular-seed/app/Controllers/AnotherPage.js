'use strict';

angular.module('myApp.AnotherPage', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
  $routeProvider.when('/AnotherPage', {
    templateUrl: 'Views/AnotherPage.html',
    controller: 'AnotherPageCtrl'
  });
}])

.controller('AnotherPageCtrl', function($scope, $location) {
	$scope.onClick = function() {
		$location.path('/StudyLandingPage');
	};
});
