/// <reference path="controllers.wizard.ts" />
/// <reference path="controllers.dash.ts" />
/// <reference path="models.wizard.ts" />
/// <reference path="controllers.pittest.ts" />
/// <reference path="services.peach.ts" />

module DashApp {
	"use strict";

	var INTEGER_REGEXP = /^\-?\d+$/;

	//8-9 pairs
	//var HEX_REGEXP = /^([A-Fa-f0-9]{2}){8,9}$/;
	var HEX_REGEXP = /^[0-9A-Fa-f]+$/;

	//	"dashServices",
	//	"dashControllers",

	var dashApp = angular.module("dashApp", [
		"ngResource",
		"emguo.poller",
		"ngGrid",
		"n3-charts.linechart",
		"ngRoute",
		"LocalStorageModule",
	])
		.service("peachService", ["$resource", "$http", ($resource, $http) => new PeachService($resource, $http)])
		.config(["$routeProvider", "$locationProvider", function ($routeProvider: ng.route.IRouteProvider, $locationProvider: ng.ILocationProvider) {
			//$locationProvider.html5Mode(true);

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
						if (INTEGER_REGEXP.test(viewValue)) {
							// it is valid
							ctrl.$setValidity('integer', true);
							return viewValue;
						} else {
							// it is invalid, return undefined (no model update)
							ctrl.$setValidity('integer', false);
							return undefined;
						}
					});
				}
			};
		})
		.directive('hexstring', function () {
			return {
				require: 'ngModel',
				link: function (scope, elm, attrs, ctrl) {
					ctrl.$parsers.unshift(function (viewValue) {
						if (HEX_REGEXP.test(viewValue)) {
							// it is valid
							ctrl.$setValidity('hexstring', true);
							return viewValue;
						} else {
							// it is invalid, return undefined (no model update)
							ctrl.$setValidity('hexstring', false);
							return undefined;
						}
					});
				}
			};
		});

	//var dashServices = angular.module('dashServices', ['ngResource']);
	//dashServices.service("peachService", PeachRestService);
} 