'use strict';

describe('DashApp controllers', function () {

    beforeEach(function(){
        // do rigging stuff up thigns here that run before each test
        // this.foo = blah
    });

    // load (or refresh) modules before testing them
    beforeEach(module('ng'));
    beforeEach(module('ngResource'));
    beforeEach(module('ngAnimate'));
    beforeEach(module('dashApp'));
    //beforeEach(module('emguoPoller'));
    //beforeEach(module('PeachRestService'));

    // tests for this particular controller
    describe('WizardController', function () {
        var scope, ctrl;

        // inject services and stuffs and override data here
        // see: angular-phonecat/test/unit/controllersSpec.js @ tag=='step-10'
        //
        // WizardController.$inject = ["$scope", "$routeParams", "$location", "peachService", "localStorageService"];
        //
        beforeEach(inject(function($rootScope, $routeParams, $location, peachService, localStorageService, $controller){
            scope = $rootScope.$new();
            ctrl  = $controller('WizardController', {$scope: scope});
        }));

        it('should just pass', function() {
           // should do stuffs
           expect(3).toBe(4-1);
        });
    });
});
