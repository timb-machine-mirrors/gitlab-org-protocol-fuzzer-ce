/// <reference path="reference.ts" />

module Peach {
	"use strict";

	var INTEGER_REGEXP = /^\-?\d+$/;
	var HEX_REGEXP = /^[0-9A-Fa-f]+$/;

	export interface IViewModelScope extends ng.IScope {
		vm: any;
	}

	export interface ITab {
		title: string;
		content: string;
		active: boolean;
		disabled: boolean;
	}

	var p = angular.module("Peach", [
		"ngResource",
		"emguo.poller",
		"ngGrid",
		"ngRoute",
		"ui.bootstrap",
		"treeControl",
		"angles",
		"ngVis"
	]);
	p.factory('PeachConfigResource', [
		'$resource',
		($resource: ng.resource.IResourceService): Models.IKeyValueResource => {
			return <Models.IKeyValueResource> $resource(
				'/p/conf/wizard/state'
			);
		}
	]);
	p.factory('PitLibraryResource', [
		'$resource',
		($resource: ng.resource.IResourceService): Models.ILibraryResource => {
			return <Models.ILibraryResource> $resource('/p/libraries');
		}
	]);
	p.factory('PitResource', [
		'$resource',
		($resource: ng.resource.IResourceService): Models.IPitResource => {
			return <Models.IPitResource> $resource(
				'/p/pits/:id', { id: '@id' }
			);
		}
	]);
	p.factory('PitConfigResource', [
		'$resource',
		($resource: ng.resource.IResourceService): Models.IPitConfigResource => {
			return <Models.IPitConfigResource> $resource(
				'/p/pits/:id/config', { id: '@id' }
			);
		}
	]);
	p.factory('PitAgentsResource', [
		'$resource',
		($resource: ng.resource.IResourceService): Models.IPitAgentsResource => {
			return <Models.IPitAgentsResource> $resource(
				'/p/pits/:id/agents', { id: '@id' }
			);
		}
	]);
	p.factory('AvailableMonitorsResource', [
		'$resource',
		($resource: ng.resource.IResourceService): Models.IMonitorResource => {
			return <Models.IMonitorResource> $resource(
				'/p/conf/wizard/monitors', {}, {
					query: { method: 'GET', isArray: true, cache: true }
				}
			);
		}
	]);
	p.factory('FaultDetailResource', [
		'$resource',
		($resource: ng.resource.IResourceService): Models.IFaultDetailResource => {
			return <Models.IFaultDetailResource> $resource(
				'/p/faults/:id', { id: '@id' }
			);
		}
	]);
	p.service('PitService', Services.PitService);
	p.service('JobService', Services.JobService);
	p.service('TestService', Services.TestService);
	p.service('WizardService', Services.WizardService);
	p.config([
		"$routeProvider",
		($routeProvider: ng.route.IRouteProvider) => {
			$routeProvider
				.when("/", {
					templateUrl: "html/dashboard.html",
					controller: DashboardController
				})
				.when("/faults/:bucket", {
					templateUrl: "html/faults.html",
					controller: FaultsController
				})
				.when("/metrics/:metric", {
					templateUrl: "html/metrics.html",
					controller: MetricsController
				})
				.when("/quickstart/intro", {
					templateUrl: "html/wizard/intro.html",
					controller: WizardController
				})
				.when("/quickstart/done", {
					templateUrl: "html/wizard/done.html",
					controller: WizardController
				})
				.when("/quickstart/test", {
					templateUrl: "html/wizard/test.html",
					controller: PitTestController
				})
				.when("/quickstart/:step", {
					templateUrl: "html/wizard.html",
					controller: WizardController
				})
				.when("/cfg/monitors", {
					templateUrl: "html/cfg/monitors.html",
					controller: ConfigureMonitorsController
				})
				.when("/cfg/variables", {
					templateUrl: "html/cfg/variables.html",
					controller: ConfigureVariablesController
				})
				.when("/cfg/test", {
					templateUrl: "html/cfg/test.html",
					controller: PitTestController
				})
				.otherwise({
					redirectTo: "/"
				});
		}
	]);
	p.directive("integer", () => {
		return {
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				ctrl.$parsers.unshift(viewValue => {
					var isIntValue = INTEGER_REGEXP.test(viewValue);
					ctrl.$setValidity('integer', isIntValue);
					if (isIntValue) {
						return viewValue;
					}
					return undefined;
				});
			}
		};
	});
	p.directive('hexstring', () => {
		return {
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				ctrl.$parsers.unshift(viewValue => {
					var isHexValue = HEX_REGEXP.test(viewValue);
					ctrl.$setValidity('hexstring', isHexValue);
					if (isHexValue) {
						return viewValue;
					}
				});
			}
		};
	});
	p.directive('ngEnter', () => {
		return {
			link: (scope: ng.IScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => {
				element.bind("keydown keypress", (event: JQueryEventObject) => {
					if (event.which === 13) {
						scope.$apply(() => {
							scope.$eval(attrs['ngEnter']);
						});

						event.preventDefault();
					}
				});
			}
		};
	});
	p.directive('ngMin', () => {
		return {
			restrict: 'A',
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				scope.$watch(attrs['ngMin'], () => {
					ctrl.$setViewValue(ctrl.$viewValue);
				});
				var minValidator = value => {
					var min = scope.$eval(attrs['ngMin']) || 0;
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
	});
	p.directive('ngMax', () => {
		return {
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				scope.$watch(attrs['ngMax'], () => {
					ctrl.$setViewValue(ctrl.$viewValue);
				});
				var maxValidator = value => {
					var max = scope.$eval(attrs['ngMax']) || Infinity;
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
	});
	p.filter('filesize', () => {
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
	});
	// hack to allow form field names to be dynamic
	// http://stackoverflow.com/questions/14378401/dynamic-validation-and-name-in-a-form-with-angularjs
	p.config([
		'$provide', $provide => {
			$provide.decorator('ngModelDirective', $delegate => {
				var ngModel = $delegate[0];
				var controller = ngModel.controller;
				ngModel.controller = [
					'$scope',
					'$element',
					'$attrs',
					'$injector',
					function(scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, $injector: ng.auto.IInjectorService) {
						var $interpolate = $injector.get('$interpolate');
						attrs.$set('name', $interpolate(attrs['name'] || '')(scope));
						$injector.invoke(controller, this, {
							'$scope': scope,
							'$element': elm,
							'$attrs': attrs
						});
					}
				];
				return $delegate;
			});
			$provide.decorator('formDirective', $delegate => {
				var form = $delegate[0];
				var controller = form.controller;
				form.controller = [
					'$scope',
					'$element',
					'$attrs',
					'$injector',
					function(scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, $injector: ng.auto.IInjectorService) {
						var $interpolate = $injector.get('$interpolate');
						attrs.$set('name', $interpolate(attrs['name'] || attrs['ngForm'] || '')(scope));
						$injector.invoke(controller, this, {
							'$scope': scope,
							'$element': elm,
							'$attrs': attrs
						});
					}
				];
				return $delegate;
			});
		}
	]);

	export function Startup() {
		window.onerror = (message, url, lineNo) => {
			console.log('Error: ' + message + '\n' + 'Line Number: ' + lineNo);
			return true;
		};

		var version = getHtmlVer();
		if (version < 5) {
			alert(
				"This application requires an HTML 5 and ECMAScript 5 capable browser. " +
				"Please upgrade your browser to a more recent version."
			);
		} else {
			initialize();
		}

		function initialize() {
			jQuery($ => {
				// shows tooltips
				$('[data-rel=tooltip]').tooltip();

				// handles disabling of navbar items
				$("ul.nav-list").on('click', 'li.disabled', false);
			});
		}

		function getHtmlVer() {
			var cName = navigator.appCodeName;
			var uAgent: any = navigator.userAgent;
			var htmlVer: any = 0.0;
			// Remove start of string in UAgent upto CName or end of string if not found.
			uAgent = uAgent.substring((uAgent + cName).toLowerCase().indexOf(cName.toLowerCase()));
			// Remove CName from start of string. (Eg. '/5.0 (Windows; U...)
			uAgent = uAgent.substring(cName.length);
			// Remove any spaves or '/' from start of string.
			while (uAgent.substring(0, 1) === " " || uAgent.substring(0, 1) === "/") {
				uAgent = uAgent.substring(1);
			}
			// Remove the end of the string from first characrer that is not a number or point etc.
			var pointer = 0;
			while ("0123456789.+-".indexOf((uAgent + "?").substring(pointer, pointer + 1)) >= 0) {
				pointer = pointer + 1;
			}
			uAgent = uAgent.substring(0, pointer);

			if (!isNaN(uAgent)) {
				if (uAgent > 0) {
					htmlVer = uAgent;
				}
			}
			return parseFloat(htmlVer);
		}
	}
}
