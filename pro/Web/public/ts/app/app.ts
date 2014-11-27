/// <reference path="reference.ts" />

module Peach {
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
		.factory('PitLibraryResource', [
			'$resource',
			($resource: ng.resource.IResourceService): Models.IPitLibraryResource => {
				return <Models.IPitLibraryResource> $resource('/p/libraries');
			}
		])
		.factory('MonitorResource', [
			'$resource',
			($resource: ng.resource.IResourceService): Models.IMonitorResource => {
				return <Models.IMonitorResource> $resource('/p/conf/wizard/monitors', {}, {
					query: { method: 'GET', isArray: true, cache: true }
				});
			}
		])
		.service("peachService", Services.PeachService)
		.service("pitConfiguratorService", Services.PitConfiguratorService)
		.config([
			"$routeProvider",
			"$locationProvider",
			($routeProvider: ng.route.IRouteProvider, $locationProvider: ng.ILocationProvider) => {
				$routeProvider
					.when("/", {
						templateUrl: "html/dash.html",
						controller: DashController
					})
					.when("/faults/:bucket", {
						templateUrl: "html/faults.html",
						controller: FaultsController
					})
					.when("/quickstart/intro", {
						templateUrl: "html/quickstart-intro.html",
						controller: WizardController
					})
					.when("/metrics/:metric", {
						templateUrl: "html/metrics.html",
						controller: MetricsController
					})
					.when("/quickstart/test", {
						templateUrl: "html/quickstart-test.html",
						controller: PitTestController
					})
					.when("/quickstart/done", {
						templateUrl: "html/quickstart-done.html",
						controller: WizardController
					})
					.when("/quickstart/:step", {
						templateUrl: "html/wizard.html",
						controller: WizardController
					})
					.when("/cfg/monitors", {
						templateUrl: "html/cfg-monitors.html",
						controller: ConfigureMonitorsController
					})
					.when("/cfg/variables", {
						templateUrl: "html/cfg-variables.html",
						controller: ConfigureVariablesController
					})
					.when("/cfg/test", {
						templateUrl: "html/cfg-test.html",
						controller: ConfigureTestController
					})
					.otherwise({
						redirectTo: "/"
					});
			}
		])
		.directive("integer", () => {
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
		.directive('ngEnter', () => {
			return (scope: ng.IScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => {
				element.bind('keypress', (evt: JQueryEventObject) => {
					if (evt.which === 13) {
						scope.$apply(() => {
							scope.$eval(attrs['ngEnter']);
						});
						evt.preventDefault();
					}
				});
			}
		})
		.run(($rootScope: ng.IRootScopeService, $templateCache: ng.ITemplateCacheService) => {
			$rootScope.$on('$routeChangeStart', (event, next, current) => {
				if (typeof (current) !== 'undefined') {
					$templateCache.remove(current.templateUrl);
				}
			});
		})
	;

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

// This hack is in place to fix autofocus issues with modal instances
// https://github.com/angular-ui/bootstrap/issues/1696
angular.module('ui.bootstrap.modal').directive('modalWindow', ($timeout: ng.ITimeoutService) => {
	return {
		priority: 1,
		link: (scope: ng.IScope, element: ng.IAugmentedJQuery) => {
			// it appears animation takes ~300ms, so wait until this is done
			// if it's too soon, the cursor will appear to float outside the bounds of the input element
			$timeout(() => {
				element.find('[autofocus]').focus();
			}, 400);
		}
	};
});

// hack to fix scroll flickering issues
// http://pushkarkinikar.wordpress.com/2014/07/10/ng-grid-scroll-flickering-issue/
// override/replace existing directives
// https://stackoverflow.com/questions/18421732/angularjs-how-to-override-directive-ngclick
angular.module('ngGrid.directives')
	.directive('ngViewport', () => {
		return {
			replace: false,
			link: ($scope: any, elm) => {
				var isMouseWheelActive;
				var prevScollLeft;
				var prevScollTop = 0;
				var ensureDigest = () => {
					if (!$scope.$root.$$phase) {
						$scope.$digest();
					}
				};
				var scrollTimer;

				function scroll(evt) {
					var scrollLeft = evt.target.scrollLeft,
						scrollTop = evt.target.scrollTop;
					if ($scope.$headerContainer) {
						$scope.$headerContainer.scrollLeft(scrollLeft);
					}
					$scope.adjustScrollLeft(scrollLeft);
					$scope.adjustScrollTop(scrollTop);
					if ($scope.forceSyncScrolling) {
						ensureDigest();
					} else {
						clearTimeout(scrollTimer);
						scrollTimer = setTimeout(ensureDigest, 150);
					}
					prevScollLeft = scrollLeft;
					prevScollTop = scrollTop;
					isMouseWheelActive = false;
					return true;
				}

				elm.bind('scroll', scroll);

				elm.on('$destroy', () => {
					elm.off('scroll', scroll);
				});

				if (!$scope.enableCellSelection) {
					$scope.domAccessProvider.selectionHandlers($scope, elm);
				}
			}
		}
	})
	.config(($provide: ng.auto.IProvideService) => {
		$provide.decorator('ngViewportDirective', [
			'$delegate', ($delegate: ng.IDirective[]) => {
				$delegate.shift();
				return $delegate;
			}
		]);
	})
;
