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
		$location.path('/LandingPage');
	};

	// function autoResizeIFrame() {
	//   $('iframe').height(
	//     function() {
	//       return $(this).contents().find('body').height() + 20;
	//     }
	//   )
	// }
	// $('iframe').contents().find('body').css({"min-height": "100", "overflow" : "hidden"});
	// setTimeout(autoResizeIFrame, 2000);
	// setTimeout(autoResizeIFrame, 10000);
});