/// <reference path="Models/models.ts" />
/// <reference path="Services/peach.ts" />
/// <reference path="Services/pitconfigurator.ts" />
/// <reference path="Controllers/copypit.ts" />
/// <reference path="Controllers/dash.ts" />
/// <reference path="Controllers/faults.ts" />
/// <reference path="Controllers/main.ts" />
/// <reference path="Controllers/metrics.ts" />
/// <reference path="Controllers/pitlibrary.ts" />
/// <reference path="Controllers/pittest.ts" />
/// <reference path="Controllers/wizard.ts" />
/// <reference path="Controllers/startjob.ts" />


module DashApp {
	"use strict";
	
	var INTEGER_REGEXP = /^\-?\d+$/;
	var HEX_REGEXP = /^[0-9A-Fa-f]+$/;

	var peachDash = angular.module("peachDash", [
		"ngResource",
		"emguo.poller",
		"ngGrid",
		"ngRoute",
		"ui.bootstrap",
		"treeControl",
		"angles",
		"ngVis"
	]).service("peachService", ["$resource", "$http", ($resource, $http) => new Services.PeachService($resource, $http)])
		.service("pitConfiguratorService", ["poller", "peachService", (poller, peachService) => new Services.PitConfiguratorService(poller, peachService)])
		.config(["$routeProvider", "$locationProvider", function ($routeProvider: ng.route.IRouteProvider, $locationProvider: ng.ILocationProvider) {

			$routeProvider
				.when("/", {
					templateUrl: "/partials/dash.html",
					controller: DashController
				})
				.when("/faults", {
					templateUrl: "/partials/faults.html",
					controller: FaultsController
				})
				.when("/configurator/intro", {
					templateUrl: "/partials/configurator-intro.html",
					controller: WizardController
				})
				.when("/metrics/:metric", {
					templateUrl: "/partials/metrics.html", 
					controller: MetricsController
				})
				.when("/configurator/test", {
					templateUrl: "/partials/configurator-test.html",
					controller: PitTestController
				})
				.when("/configurator/done", {
					templateUrl: "/partials/configurator-done.html",
					controller: WizardController
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
						else return undefined;
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
						else return undefined;
					});
				}
			};
		})
		.directive('ngEnter', function () {
			return {
				link: function (scope, element, attrs, ctrl) {
					element.bind("keydown keypress", function (event) {
						if (event.which === 13) {
							scope.$apply(function () {
								scope.$eval(attrs.ngEnter);
							});

							event.preventDefault();
						}
					});
				}
			}
		})
		.directive('ngMin', function () {
			return {
				restrict: 'A',
				require: 'ngModel',
				link: function (scope, elem, attr, ctrl) {
					scope.$watch(attr.ngMin, function () {
						ctrl.$setViewValue(ctrl.$viewValue);
					});
					var minValidator = function (value) {
						var min = scope.$eval(attr.ngMin) || 0;
						if (!isEmpty(value) && value < min) {
							ctrl.$setValidity('ngMin', false);
							return undefined;
						} else {
							ctrl.$setValidity('ngMin', true);
							return value;
						}
					};

					ctrl.$parsers.push(minValidator);
					ctrl.$formatters.push(minValidator);
				}
			};
		})
		.directive('ngMax', function () {
			return {
				require: 'ngModel',
				link: function (scope, elem, attr, ctrl) {
					scope.$watch(attr.ngMax, function () {
						ctrl.$setViewValue(ctrl.$viewValue);
					});
					var maxValidator = function (value) {
						var max = scope.$eval(attr.ngMax) || Infinity;
						if (!isEmpty(value) && value > max) {
							ctrl.$setValidity('ngMax', false);
							return undefined
						} else {
							ctrl.$setValidity('ngMax', true);
							return value;
						}
					};

					ctrl.$parsers.push(maxValidator);
					ctrl.$formatters.push(maxValidator);
				}
			};
		})
		.run(function ($rootScope, $templateCache) {
			$rootScope.$on('$routeChangeStart', function (event, next, current) {
				if (typeof(current) !== 'undefined') {
					$templateCache.remove(current.templateUrl);
				}
			});
		});
	function isEmpty(value) {
		return angular.isUndefined(value) || value === '' || value === null || value !== value;
	}
}

