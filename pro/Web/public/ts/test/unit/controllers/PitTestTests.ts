/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('PitTestController', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $interval: ng.IIntervalService;
		var ctrl: Peach.PitTestController;

		var pitId = 'PIT_GUID';
		var pitUrl = C.Api.PitUrl.replace(':id', pitId);
		var pit = <Peach.IPit> {
			id: pitId,
			pitUrl: pitUrl,
			name: 'My Pit',
			versions: [
				{
					version: 0,
					configured: false,
					locked: false
				}
			]
		};

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $state = <ng.ui.IStateService> $injector.get(C.Angular.$state);
			var $rootScope = <ng.IRootScopeService> $injector.get(C.Angular.$rootScope);
			var $controller = <ng.IControllerService> $injector.get(C.Angular.$controller);
			var $templateCache = <ng.ITemplateCacheService> $injector.get(C.Angular.$templateCache);
			var pitService = <Peach.PitService> $injector.get(C.Services.Pit);

			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$interval = $injector.get(C.Angular.$interval);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Pit.Configure, '');

			$state.go(C.States.Pit, { pit: pitId });
			$rootScope.$digest();

			$httpBackend.whenGET(pitUrl).respond(pit);
			pitService.LoadPit();
			$httpBackend.flush();

			ctrl = $controller('PitTestController', {
				$scope: $rootScope.$new()
			});
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		it("new", () => {
			expect(_.isObject(ctrl)).toBe(true);
		});

		it("can begin a test", () => {
			var testUrl = '/p/my/test/url';
			var req: Peach.IJobRequest = {
				pitUrl: pitUrl,
				isControlIteration: true
			};
			var job: Peach.IJob = {
				id: 'JOB_ID',
				pitUrl: pitUrl,
				firstNodeUrl: testUrl
			};
			$httpBackend.expectPOST(Peach.C.Api.Jobs, req).respond(job);
			ctrl.OnBeginTest();
			$httpBackend.flush();

			var result1: Peach.ITestResult = {
				status: 'active',
				log: '',
				events: []
			};

			$httpBackend.expectGET(testUrl).respond(result1);
			$interval.flush(Peach.TEST_INTERVAL);
			$httpBackend.flush();

			expect(ctrl.CanBeginTest).toBe(false);
			expect(ctrl.CanContinue).toBe(false);

			var result2: Peach.ITestResult = {
				status: 'pass',
				log: '',
				events: []
			};
			pit.versions[0].configured = true;

			$httpBackend.expectGET(testUrl).respond(result2);
			$interval.flush(Peach.TEST_INTERVAL);
			$httpBackend.flush();

			expect(ctrl.CanBeginTest).toBe(true);
			expect(ctrl.CanContinue).toBe(true);
		});
	});
});
