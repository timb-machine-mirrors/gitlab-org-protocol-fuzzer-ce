/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('Dashboard controller', () => {
		var ctrl: Peach.DashboardController;
		//var service: Peach.Services.PitService;
		var $modal: ng.ui.bootstrap.IModalService;
		var service = {
			IsConfigured: false
		};

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$modal = $injector.get('$modal');
			var $rootScope = <ng.IRootScopeService> $injector.get('$rootScope');
			var $controller = <ng.IControllerService> $injector.get('$controller');

			ctrl = $controller('Peach.DashboardController', {
				$scope: $rootScope.$new(),
				$modal: $modal,
				PitService: service
			});
		}));

		it("new", () => {
			expect(_.isObject(ctrl)).toBe(true);
			expect(ctrl.IsConfigured).toBe(false);
		});
	});
});
