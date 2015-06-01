/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('Dashboard controller', () => {
		var ctrl: Peach.DashboardController;
		var $modal: ng.ui.bootstrap.IModalService;
		var pitService = {
			IsConfigured: false,
			Pit: {}
		};

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$modal = $injector.get(Peach.C.Angular.$modal);
			var $rootScope = <ng.IRootScopeService> $injector.get(Peach.C.Angular.$rootScope);
			var $controller = <ng.IControllerService> $injector.get(Peach.C.Angular.$controller);
			var jobService = $injector.get(Peach.C.Services.Job);

			ctrl = $controller('DashboardController', {
				$scope: $rootScope.$new(),
				$modal: $modal,
				PitService: pitService,
				JobService: jobService
			});
		}));

		it("new", () => {
			expect(_.isObject(ctrl)).toBe(true);
		});
	});
});
