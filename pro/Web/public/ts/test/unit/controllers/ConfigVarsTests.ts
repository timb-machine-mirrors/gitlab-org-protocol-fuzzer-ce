/// <reference path="../reference.ts" />

describe("Peach", () => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('ConfigureVariablesController', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $controller: ng.IControllerService;
		var $scope;

		var service: Peach.PitService;
		var ctrl: Peach.ConfigureVariablesController;

		var pitId = 'PIT_GUID';
		var pitUrl = C.Api.PitUrl.replace(':id', pitId);
		var pit = {
			id: pitId,
			name: 'My Pit',
			pitUrl: pitUrl,
			config: [
				{ key: 'Key', name: 'Name', value: 'Value' }
			]
		};

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $rootScope = <ng.IRootScopeService> $injector.get(C.Angular.$rootScope);
			$controller = <ng.IControllerService> $injector.get(C.Angular.$controller);
			$scope = $rootScope.$new();
			var $templateCache = $injector.get(C.Angular.$templateCache);
			var $state = <ng.ui.IStateService> $injector.get(C.Angular.$state);
			$httpBackend = $injector.get(C.Angular.$httpBackend);
			service = $injector.get(C.Services.Pit);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Error, '');

			$state.params['pit'] = pitId;

			$httpBackend.whenGET(pitUrl).respond(pit);
			ctrl = $controller('ConfigureVariablesController', {
				$scope: $scope,
				PitService: service
			});
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		it("new", () => {
			expect(_.isObject(ctrl)).toBe(true);
		});

		it("PitConfig is valid", () => {
			expect(_.isObject(ctrl.Config)).toBe(true);
		});

		it("PitConfig can be saved", () => {
			var dirty = true;
			$scope['form'] = {
				$setPristine: () => {
					dirty = false;
				}
			};

			$httpBackend.expectPOST(pitUrl).respond(pit);
			ctrl.OnSave();
			$httpBackend.flush();
			expect(dirty).toBe(false);
		});
	});
});
