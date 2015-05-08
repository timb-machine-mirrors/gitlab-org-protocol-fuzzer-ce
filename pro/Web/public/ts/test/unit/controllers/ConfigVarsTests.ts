﻿/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('ConfigureVariablesController', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $modal: ng.ui.bootstrap.IModalService;
		var $controller: ng.IControllerService;
		var $rootScope: ng.IRootScopeService;
		var $scope;

		var service: Peach.PitService;
		var ctrl: Peach.ConfigureVariablesController;
		var pitUrl = '/p/pits/PIT_GUID';

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$modal = $injector.get(Peach.C.Angular.$modal);
			$rootScope = <ng.IRootScopeService> $injector.get(Peach.C.Angular.$rootScope);
			$controller = <ng.IControllerService> $injector.get(Peach.C.Angular.$controller);
			$scope = $rootScope.$new();

			$httpBackend = $injector.get(Peach.C.Angular.$httpBackend);
			service = $injector.get(Peach.C.Services.Pit);
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		describe("when a Pit is not selected", () => {
			beforeEach(() => {
				ctrl = $controller('ConfigureVariablesController', {
					$scope: $scope,
					$modal: $modal,
					PitService: service
				});
			});

			it("new", () => {
				expect(_.isObject(ctrl)).toBe(true);
			});

			it("PitConfig is undefined", () => {
				expect(ctrl.Config).toBeUndefined();
			});
		});

		describe("when a Pit is selected", () => {
			var pit;
			beforeEach(() => {
				pit = {
					name: 'My Pit',
					pitUrl: pitUrl,
					config: [
						{ key: 'Key', name: 'Name', value: 'Value' }
					]
				};

				$httpBackend.expectGET(pitUrl).respond(pit);
				var promise = service.SelectPit(pitUrl);
				promise.then(() => {
					$httpBackend.expectGET(pitUrl).respond(pit);
					ctrl = $controller('ConfigureVariablesController', {
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
});
