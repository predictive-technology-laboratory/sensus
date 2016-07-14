'use strict';
	
angular.module('myApp', [
	  	'ngRoute',
	  	'myApp.LandingPage',
	  	'myApp.AnotherPage',
	  	'myApp.version'
	])
.config(['$routeProvider', function($routeProvider) {
  	$routeProvider.otherwise({redirectTo: '/LandingPage'});
}]);