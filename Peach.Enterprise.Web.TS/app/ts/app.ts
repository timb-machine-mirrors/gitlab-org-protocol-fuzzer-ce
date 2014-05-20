/// <reference path="controllers.wizard.ts" />
/// <reference path="controllers.dash.ts" />
/// <reference path="models.wizard.ts" />
/// <reference path="controllers.pittest.ts" />
/// <reference path="services.peach.ts" />

module DashApp {
	"use strict";

	var INTEGER_REGEXP = /^\-?\d+$/;
	var HEX_REGEXP = /^[0-9A-Fa-f]+$/;

	var dashApp = angular.module("dashApp", [
		"ngResource",
		"emguo.poller",
		"ngGrid",
		"n3-charts.linechart",
		"ngRoute",
		"LocalStorageModule",
	])
		.service("peachService", ["$resource", ($resource) => new PeachService($resource)])
        .config(["$routeProvider", "$locationProvider", function ($routeProvider: ng.route.IRouteProvider, $locationProvider: ng.ILocationProvider) {

			$routeProvider
				.when("/", {
					templateUrl: "/partials/dash.html",
					controller: DashController
				})
				.when("/metrics", {
					templateUrl: "/partials/metrics.html",
					controller: MetricsController
				})
				.when("/faults", {
					templateUrl: "/partials/faults.html",
					controller: FaultsController
				})
				.when("/configurator/intro", {
					templateUrl: "/partials/configurator-intro.html"
				})
				.when("/configurator/test", {
					templateUrl: "/partials/configurator-test.html",
					controller: PitTestController
				})
				.when("/configurator/done", {
					templateUrl: "/partials/configurator-done.html"
				})
				.when("/configurator/:step", {
					templateUrl: "/partials/wizard.html",
					controller: WizardController
				})
				.otherwise({
					redirectTo: "/"
				});
		}])
	  .directive('integer', function () {
			return {
				require: 'ngModel',
				link: function (scope, elm, attrs, ctrl) {
					ctrl.$parsers.unshift(function (viewValue) {
						var isIntValue = INTEGER_REGEXP.test(viewValue);
						ctrl.$setValidity('integer', isIntValue);
						if (isIntValue) return viewValue;
						else            return undefined;
					});
				}
			};
		})
		.directive('hexstring', function () {
			return {
				require: 'ngModel',
				link: function (scope, elm, attrs, ctrl) {
					ctrl.$parsers.unshift(function (viewValue) {
						var isHexValue = HEX_REGEXP.test(viewValue);
						ctrl.$setValidity('hexstring', isHexValue);
						if (isHexValue) return viewValue;
						else            return undefined;
					});
				}
			};
		});
} 
