'use strict';

describe('DashApp controllers', function () {

    beforeEach(function(){
        // do rigging stuff up thigns here that run before each test
        // this.foo = blah
    });

    // load (or refresh) modules before testing them
    beforeEach(module('DashApp'));
    beforeEach(module('PeachRestService'));

    // tests for this particular controller
    describe('FaultsAppCtrl', function () {
        var foo, bar, baz, fooData = function() { return {'oh': 'hai'} };

        // inject services and stuffs and override data here
        // see: angular-phonecat/test/unit/controllersSpec.js @ tag=='step-10'
        beforeEach(inject(function(<things to inject>){
        }));

        it('<describe test>', function() {
           //should do stuffs 
           expect(3).toBe(4-1);
        });
    });
});
