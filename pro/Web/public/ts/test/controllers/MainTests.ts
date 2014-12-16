/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('Main controller', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $location: ng.ILocationService;
		var $window: ng.IWindowService;
		var ctrl: Peach.MainController;
		var service: Peach.PitService;
		var pitUrl = '/p/pits/PIT_GUID';
		var $modal: ng.ui.bootstrap.IModalService;
		var spyOpen: jasmine.Spy;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$window = $injector.get('$window');
			$window.sessionStorage.clear();

			$modal = $injector.get('$modal');
			spyOpen = spyOn($modal, 'open');

			$httpBackend = $injector.get('$httpBackend');
			$httpBackend.expectGET('/p/libraries').respond([
				{ libraryUrl: '', locked: false }
			]);
			service = $injector.get('PitService');
			$httpBackend.flush();

			var $rootScope = <ng.IRootScopeService> $injector.get('$rootScope');
			var $controller = <ng.IControllerService> $injector.get('$controller');
			$location = $injector.get('$location');

			spyOpen.and.returnValue({
				result: {
					then: () => { }
				}
			});

			$httpBackend.expectGET('/p/jobs').respond([]);
			ctrl = $controller('Peach.MainController', {
				$scope: $rootScope.$new(),
				$window: $window,
				$location: $location,
				$modal: $modal,
				PitService: service
			});
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		describe('when a Pit is not selected', () => {
			it("new", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(ctrl.IsActive("/quickstart/intro")).toBe(false);
			});

			it("get PitName is (none)", () => {
				expect(ctrl.PitName).toBe('(none)');
			});

			it("should not start the wizard", () => {
				expect(ctrl.IsActive("/quickstart/intro")).toBe(false);
			});
		});

		describe('select a Pit that needs to be configured', () => {
			var pit: Peach.IPit;
			beforeEach(() => {
				pit = <Peach.IPit> {
					pitUrl: pitUrl,
					name: 'My Pit',
					versions: [{ configured: false }]
				};

				spyOpen.and.returnValue({
					result: {
						then: (callback) => {
							var promise = service.SelectPit(pitUrl);
							promise.then(() => {
								callback();
							});
						}
					}
				});

				$httpBackend.expectGET(pitUrl).respond(pit);
				ctrl.OnSelectPit();
				$httpBackend.flush();
			});

			it("should load the pit", () => {
				expect($modal.open).toHaveBeenCalled();
				expect(_.isObject(service.Pit)).toBe(true);
				expect(ctrl.PitName).toBe(pit.name);
			});

			it("should start the wizard", () => {
				expect($location.path()).toBe("/quickstart/intro");
				expect(ctrl.IsActive("/quickstart/intro")).toBe(true);
			});
		});

		describe('select a Pit that is already configured', () => {
			beforeEach(() => {
				var pit = {
					pitUrl: pitUrl,
					name: 'My Pit',
					versions: [{ configured: true }]
				};

				spyOpen.and.returnValue({
					result: {
						then: (callback) => {
							var promise = service.SelectPit(pitUrl);
							promise.then(() => {
								callback();
							});
						}
					}
				});

				$httpBackend.expectGET(pitUrl).respond(pit);
				ctrl.OnSelectPit();
				$httpBackend.flush();
			});

			it("should load the pit", () => {
				expect($modal.open).toHaveBeenCalled();
				expect(_.isObject(service.Pit)).toBe(true);
			});

			it("should not start the wizard", () => {
				expect($location.path()).not.toBe("/quickstart/intro");
				expect(ctrl.IsActive("/quickstart/intro")).toBe(false);
			});
		});
	});
});
