'use strict';
	
angular.module('myApp', [
	  	'ngRoute',
		'myApp.LoginPage',
		'myApp.CreateResearcherPage',
		'myApp.StudyLandingPage',
		'myApp.CreateStudyPage',
		'myApp.AnotherPage',
	  	'myApp.version'
	])
.config(['$routeProvider', function($routeProvider) {
  	$routeProvider.otherwise({redirectTo: '/LoginPage'});
}]);
