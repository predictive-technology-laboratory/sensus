'use strict';

describe('myApp.AnotherPage module', function() {

  beforeEach(module('myApp.AnotherPage'));

  describe('AnotherPage controller', function(){

    it('should ....', inject(function($controller) {
      //spec body
      var AnotherPageCtrl = $controller('AnotherPageCtrl');
      expect(AnotherPageCtrl).toBeDefined();
    }));

  });
});