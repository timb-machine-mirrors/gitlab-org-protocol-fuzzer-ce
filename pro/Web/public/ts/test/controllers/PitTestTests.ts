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
		var testService: Peach.Services.TestService;
		var wizardService: Peach.Services.WizardService;
		var pitService: Peach.Services.PitService;

		var pitUrl = '/p/pits/PIT_GUID';
		var pit = {
			name: 'My Pit',
			pitUrl: pitUrl
		}

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $controller: ng.IControllerService;
			var $rootScope: ng.IRootScopeService;

			$httpBackend = $injector.get('$httpBackend');
			$location = $injector.get('$location');
			$rootScope = $injector.get('$rootScope');
			$controller = $injector.get('$controller');
			$httpBackend = $injector.get('$httpBackend');
			$interval = $injector.get('$interval');

			$httpBackend.expectGET('/p/libraries').respond([
				{ libraryUrl: '', locked: false }
			]);
			pitService = $injector.get('PitService');
			wizardService = $injector.get('WizardService');
			testService = $injector.get('TestService');

			$httpBackend.expectGET(pitUrl).respond(pit);
			pitService.SelectPit(pitUrl);
			$httpBackend.flush();

			newController = () => {
				return $controller('Peach.PitTestController', {
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
			var ref: Peach.Models.ITestRef = {
				testUrl: '/p/my/test/url'
			};
			$httpBackend.expectGET(new RegExp('/p/conf/wizard/test/start(.*)')).respond(ref);
			ctrl.OnBeginTest();
			$httpBackend.flush();

			var result1: Peach.Models.ITestResult = {
				status: 'active',
				log: '',
				events: []
			};

			$httpBackend.expectGET(new RegExp('/p/my/test/url')).respond(result1);
			$interval.flush(Peach.Services.TEST_INTERVAL);
			$httpBackend.flush();

			var result2: Peach.Models.ITestResult = {
				status: 'pass',
				log: '',
				events: []
			};

			$httpBackend.expectGET(new RegExp('/p/my/test/url')).respond(result2);
			$interval.flush(Peach.Services.TEST_INTERVAL);
			$httpBackend.flush();

			expect(pitService.IsConfigured).toBe(true);
		});
	});
});
