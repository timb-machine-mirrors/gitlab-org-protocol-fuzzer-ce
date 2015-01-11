/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('PitTestController', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $location: ng.ILocationService;
		var $interval: ng.IIntervalService;

		var newController: () => Peach.PitTestController;
		var ctrl: Peach.PitTestController;
		var testService: Peach.TestService;
		var wizardService: Peach.WizardService;
		var pitService: Peach.PitService;

		var pitUrl = '/p/pits/PIT_GUID';

		var pit = <Peach.IPit> {
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
			var $controller: ng.IControllerService;
			var $rootScope: ng.IRootScopeService;

			$httpBackend = $injector.get('$httpBackend');
			$location = $injector.get('$location');
			$rootScope = $injector.get('$rootScope');
			$controller = $injector.get('$controller');
			$interval = $injector.get('$interval');

			pitService = $injector.get('PitService');
			wizardService = $injector.get('WizardService');
			testService = $injector.get('TestService');

			$httpBackend.expectGET(pitUrl).respond(pit);
			pitService.SelectPit(pitUrl);
			$httpBackend.flush();

			newController = () => {
				return $controller('PitTestController', {
					$scope: $rootScope.$new(),
					$location: $location,
					TestService: testService,
					WizardService: wizardService
				});
			}

			ctrl = newController();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		it("new", () => {
			expect(_.isObject(ctrl)).toBe(true);
			expect(ctrl.IsComplete("fault")).toBeFalsy();
		});

		it("can begin a test", () => {
			var testUrl = '/p/my/test/url';
			var ref: Peach.ITestRef = {
				testUrl: testUrl
			};
			$httpBackend.expectGET(new RegExp('/p/conf/wizard/test/start(.*)')).respond(ref);
			ctrl.OnBeginTest();
			$httpBackend.flush();

			var result1: Peach.ITestResult = {
				status: 'active',
				log: '',
				events: []
			};

			$httpBackend.expectGET(new RegExp(testUrl)).respond(result1);
			$interval.flush(Peach.TEST_INTERVAL);
			$httpBackend.flush();

			var result2: Peach.ITestResult = {
				status: 'pass',
				log: '',
				events: []
			};
			pit.versions[0].configured = true;

			$httpBackend.expectGET(new RegExp(testUrl)).respond(result2);
			$httpBackend.expectGET(pitUrl).respond(pit);
			$interval.flush(Peach.TEST_INTERVAL);
			$httpBackend.flush();

			expect(pitService.IsConfigured).toBe(true);
		});
	});
});
