/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('ConfigureVariablesController', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $modal: ng.ui.bootstrap.IModalService;
		var $controller: ng.IControllerService;
		var $rootScope: ng.IRootScopeService;
		var $scope;

		var service: Peach.Services.PitService;
		var ctrl: Peach.ConfigureVariablesController;
		var pitUrl = '/p/pits/PIT_GUID';

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$modal = $injector.get('$modal');
			$rootScope = <ng.IRootScopeService> $injector.get('$rootScope');
			$controller = <ng.IControllerService> $injector.get('$controller');
			$scope = $rootScope.$new();

			$httpBackend = $injector.get('$httpBackend');
			$httpBackend.expectGET('/p/libraries').respond([
				{ libraryUrl: '', locked: false }
			]);
			service = $injector.get('PitService');
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		describe("when a Pit is not selected", () => {
			beforeEach(() => {
				ctrl = $controller('Peach.ConfigureVariablesController', {
					$scope: $scope,
					$modal: $modal,
					PitService: service
				});
			});

			it("new", () => {
				expect(_.isObject(ctrl)).toBe(true);
			});

			it("PitConfig is undefined", () => {
				expect(ctrl.PitConfig).toBeUndefined();
			});
		});

		describe("when a Pit is selected", () => {
			var pit;
			beforeEach(() => {
				pit = {
					name: 'My Pit',
					pitUrl: pitUrl
				}

				$httpBackend.expectGET(pitUrl).respond(pit);
				var promise = service.SelectPit(pitUrl);
				promise.then(() => {
					$httpBackend.expectGET(pitUrl + '/config').respond(
						{
							pitUrl: pitUrl,
							config: [
								{ key: 'Key', name: 'Name', value: 'Value' }
							]
						}
					);
					ctrl = $controller('Peach.ConfigureVariablesController', {
						$scope: $scope,
						$modal: $modal,
						PitService: service
					});
				});
				$httpBackend.flush();
			});

			it("new", () => {
				expect(_.isObject(ctrl)).toBe(true);
			});

			it("PitConfig is valid", () => {
				expect(_.isObject(ctrl.PitConfig)).toBe(true);
				expect(ctrl.PitConfig.pitUrl).toBe(pitUrl);
			});

			it("PitConfig can be saved", () => {
				$scope['form'] = {
					$dirty: true
				}

				$httpBackend.expectPOST(pitUrl + '/config').respond({});
				ctrl.OnSave();
				$httpBackend.flush();
				expect($scope['form'].$dirty).toBe(false);
			});
		});
	});
});
