/// <reference path="reference.ts" />

module Peach {
	"use strict";

	var INTEGER_REGEXP = /^\-?\d+$/;
	var HEX_REGEXP = /^[0-9A-Fa-f]+$/;

	var p = angular.module("Peach", [
		"ngResource",
		"ngRoute",
		"ngGrid",
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

	p.directive('peachAgent', () => new AgentDirective());
	p.directive('peachMonitor', () => new MonitorDirective());
	p.directive('peachParameter', () => new ParameterDirective());
	p.directive('peachTest', () => new TestDirective());

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
					templateUrl: "html/cfg/monitoring.html",
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
				.when("/scratch", {
					templateUrl: "html/scratch.html",
					controller: ScratchController
				})
				.otherwise({
					redirectTo: "/"
				});
		}
	]);

	function regexValidate(
		name: string,
		pattern: RegExp,
		scope: ng.IScope,
		elm: ng.IAugmentedJQuery,
		attrs: ng.IAttributes,
		ctrl: ng.INgModelController
	) {
		ctrl.$parsers.unshift(viewValue => {
			var match = pattern.test(viewValue);
			ctrl.$setValidity(name, match);
			if (match) {
				return viewValue;
			}
			return undefined;
		});
	}

	p.directive("integer", () => {
		return {
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				return regexValidate('integer', INTEGER_REGEXP, scope, elm, attrs, ctrl);
			}
		};
	});

	p.directive('hexstring', () => {
		return {
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				return regexValidate('hexstring', HEX_REGEXP, scope, elm, attrs, ctrl);
			}
		};
	});

	p.directive('ngEnter', () => {
		return {
			restrict: 'A',
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

	p.directive('unique', () => {
		return {
			restrict: 'A',
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				var validate = value => {
					var expression = attrs['unique'];
					var list = scope.$eval(expression);
					var unique = !_.contains(list, value);
					ctrl.$setValidity('unique', unique);
					return unique ? value : undefined;
				};

				var watch = attrs['uniqueWatch'];
				if (watch) {
					scope.$watch(watch, () => {
						var model = attrs['ngModel'];
						var value = scope.$eval(model);
						validate(value);
					});
				}

				ctrl.$parsers.unshift(validate);
			}
		}
	});

	function boundsValidate(
		name: string,
		scope: ng.IScope,
		elm: ng.IAugmentedJQuery,
		attrs: ng.IAttributes,
		ctrl: ng.INgModelController
	) {
		var isMax = (name == 'ngMax');
		scope.$watch(attrs[name], () => {
			ctrl.$setViewValue(ctrl.$viewValue);
		});
		var validator = value => {
			var bound = scope.$eval(attrs[name]) || 0;
			if (!isEmpty(value) && ((isMax && value > bound) || (!isMax && value < bound))) {
				ctrl.$setValidity(name, false);
				return undefined;
			} else {
				ctrl.$setValidity(name, true);
				return value;
			}
		};

		ctrl.$parsers.push(validator);
		ctrl.$formatters.push(validator);
	}

	p.directive('ngMin', () => {
		return {
			restrict: 'A',
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				boundsValidate('ngMin', scope, elm, attrs, ctrl);
			}
		};
	});

	p.directive('ngMax', () => {
		return {
			restrict: 'A',
			require: 'ngModel',
			link: (scope: ng.IScope, elm: ng.IAugmentedJQuery, attrs: ng.IAttributes, ctrl: ng.INgModelController) => {
				boundsValidate('ngMax', scope, elm, attrs, ctrl);
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

			if (_.isUndefined(precision)) {
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
