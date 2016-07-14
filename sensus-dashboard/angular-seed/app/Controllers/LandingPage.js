'use strict';

angular.module('myApp.LandingPage', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
  $routeProvider.when('/LandingPage', {
    templateUrl: 'Views/LandingPage.html',
    controller: 'LandingPageCtrl'
  });
}])

.controller('LandingPageCtrl', function($scope, $location) {
	$scope.onClick = function() {
		$location.path('/AnotherPage');
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