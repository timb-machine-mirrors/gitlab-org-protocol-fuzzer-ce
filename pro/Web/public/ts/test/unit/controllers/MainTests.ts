/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('Main controller', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $modal: ng.ui.bootstrap.IModalService;
		var $state: ng.ui.IStateService;
		var ctrl: Peach.MainController;
		var service: Peach.PitService;
		var pitUrl = '/p/pits/PIT_GUID';
		var spyOpen: jasmine.Spy;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $controller: ng.IControllerService;
			var $rootScope: ng.IRootScopeService;
			var $templateCache: ng.ITemplateCacheService;
			var $window: ng.IWindowService;

			$controller = $injector.get(C.Angular.$controller);
			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$modal = $injector.get(C.Angular.$modal);
			$rootScope = $injector.get(C.Angular.$rootScope);
			$state = $injector.get(C.Angular.$state);
			$templateCache = $injector.get(C.Angular.$templateCache);
			$window = $injector.get(C.Angular.$window);
			service = $injector.get(C.Services.Pit);

			$window.sessionStorage.clear();

			spyOpen = spyOn($modal, 'open');
			spyOpen.and.returnValue({
				result: {
					then: () => { }
				}
			});

			$templateCache.put(C.Templates.Dashboard, '');
			$templateCache.put(C.Templates.Wizard.Intro, '');

			$httpBackend.expectGET(C.Api.Jobs).respond([]);
			ctrl = $controller('MainController', {
				$scope: $rootScope.$new()
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
				expect($state.is(C.States.Home)).toBe(true);
			});

			it("SelectPitPrompt is 'Select a Pit'", () => {
				expect(ctrl.SelectPitPrompt).toBe('Select a Pit');
			});

			it("should not start the wizard", () => {
				expect($state.is(C.States.Wizard, { step: C.Tracks.Intro })).toBe(false);
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
				expect(ctrl.SelectPitPrompt).toBe(pit.name);
			});

			it("should start the wizard", () => {
				expect($state.is(C.States.Wizard, { track: C.Tracks.Intro })).toBe(true);
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
				expect($state.is(C.States.Wizard, { step: C.Tracks.Intro })).toBe(false);
			});
		});
	});
});
