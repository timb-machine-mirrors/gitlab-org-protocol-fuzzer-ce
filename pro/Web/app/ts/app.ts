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
/// <reference path="Controllers/config-monitors.ts" />
/// <reference path="Controllers/config-test.ts" />
/// <reference path="Controllers/config-variables.ts" />

module DashApp {
	"use strict";

	var INTEGER_REGEXP = /^\-?\d+$/;
	var HEX_REGEXP = /^[0-9A-Fa-f]+$/;

	angular.module("peachDash", [
		"ngResource",
		"emguo.poller",
		"ngGrid",
		"ngRoute",
		"ui.bootstrap",
		"treeControl",
		"angles",
		"ngVis"
	])
		.service("peachService", [
			"$resource",
			"$http",
			($resource, $http) => new Services.PeachService($resource, $http)
		])
		.service("pitConfiguratorService", [
			"poller",
			"peachService",
			(poller, peachService) => new Services.PitConfiguratorService(poller, peachService)
		])
		.config([
			"$routeProvider",
			"$locationProvider",
			($routeProvider: ng.route.IRouteProvider, $locationProvider: ng.ILocationProvider) => {
				$routeProvider
					.when("/", {
						templateUrl: "/partials/dash.html",
						controller: DashController
					})
					.when("/faults/:bucket", {
						templateUrl: "/partials/faults.html",
						controller: FaultsController
					})
					.when("/quickstart/intro", {
						templateUrl: "/partials/quickstart-intro.html",
						controller: WizardController
					})
					.when("/metrics/:metric", {
						templateUrl: "/partials/metrics.html",
						controller: MetricsController
					})
					.when("/quickstart/test", {
						templateUrl: "/partials/quickstart-test.html",
						controller: PitTestController
					})
					.when("/quickstart/done", {
						templateUrl: "/partials/quickstart-done.html",
						controller: WizardController
					})
					.when("/quickstart/:step", {
						templateUrl: "/partials/wizard.html",
						controller: WizardController
					})
					.when("/configuration/monitors", {
						templateUrl: "/partials/configuration-monitors.html",
						controller: ConfigurationMonitorsController
					})
					.when("/configuration/variables", {
						templateUrl: "/partials/configuration-variables.html",
						controller: ConfigurationVariablesController
					})
					.when("/configuration/test", {
						templateUrl: "/partials/configuration-test.html",
						controller: ConfigurationTestController
					})
					.otherwise({
						redirectTo: "/"
					})
				;
			}
		])
		.directive('integer', () => {
			return {
				require: 'ngModel',
				link: (scope, elm, attrs, ctrl) => {
					ctrl.$parsers.unshift(viewValue => {
						var isIntValue = INTEGER_REGEXP.test(viewValue);
						ctrl.$setValidity('integer', isIntValue);
						if (isIntValue) return viewValue;
						else return undefined;
					});
				}
			};
		})
		.directive('hexstring', () => {
			return {
				require: 'ngModel',
				link: (scope, elm, attrs, ctrl) => {
					ctrl.$parsers.unshift(viewValue => {
						var isHexValue = HEX_REGEXP.test(viewValue);
						ctrl.$setValidity('hexstring', isHexValue);
						if (isHexValue) return viewValue;
						else return undefined;
					});
				}
			};
		})
		.directive('ngEnter', () => {
			return {
				link: (scope, element, attrs: IEnterAttributes, ctrl) => {
					element.bind("keydown keypress", event => {
						if (event.which === 13) {
							scope.$apply(() => {
								scope.$eval(attrs.ngEnter);
							});

							event.preventDefault();
						}
					});
				}
			};
		})
		.directive('ngMin', () => {
			return {
				restrict: 'A',
				require: 'ngModel',
				link: (scope, elem, attr: IMinAttributes, ctrl) => {
					scope.$watch(attr.ngMin, () => {
						ctrl.$setViewValue(ctrl.$viewValue);
					});
					var minValidator = value => {
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
		.directive('ngMax', () => {
			return {
				require: 'ngModel',
				link: (scope, elem, attr: IMaxAttributes, ctrl) => {
					scope.$watch(attr.ngMax, () => {
						ctrl.$setViewValue(ctrl.$viewValue);
					});
					var maxValidator = value => {
						var max = scope.$eval(attr.ngMax) || Infinity;
						if (!isEmpty(value) && value > max) {
							ctrl.$setValidity('ngMax', false);
							return undefined;
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
		.filter('filesize', () => {
			var units = [
				'bytes',
				'KB',
				'MB',
				'GB',
				'TB',
				'PB'
			];

			return (bytes, precision) => {
				if (bytes === 0) {
					return '0 bytes';
				}

				if (isNaN(parseFloat(bytes)) || !isFinite(bytes)) {
					return "?";
				}

				if (typeof precision === 'undefined') {
					precision = 1;
				}

				var unit = 0;

				while (bytes >= 1024) {
					bytes /= 1024;
					unit++;
				}

				var value = bytes.toFixed(precision);
				return (value.match(/\.0*$/) ? value.substr(0, value.indexOf('.')) : value) + ' ' + units[unit];
			};
		})
		.run(($rootScope, $templateCache) => {
			$rootScope.$on('$routeChangeStart', (event, next, current) => {
				if (typeof (current) !== 'undefined') {
					$templateCache.remove(current.templateUrl);
				}
			});
		});

	function isEmpty(value) {
		return angular.isUndefined(value) || value === '' || value === null || value !== value;
	}

	interface IEnterAttributes extends ng.IAttributes {
		ngEnter: any;
	}

	interface IMinAttributes extends ng.IAttributes {
		ngMin: any;
	}

	interface IMaxAttributes extends ng.IAttributes {
		ngMax: any;
	}
}
