/// <reference path="reference.ts" />

module Peach {
	"use strict";

	function getComponentName(name: string, component: IComponent): string {
		var id = component.ComponentID;
		return _.isUndefined(id) ? name : id;
	}

	function registerModule(ns, app: ng.IModule) {
		_.forOwn(ns, (component, key: string) => {
			var name = getComponentName(key, component);
			if (key.endsWith('Controller')) {
				//console.log('Registering controller', name, key);
				app.controller(name, component);
			}
			if (key.endsWith('Directive')) {
				//console.log('Registering directive', name, key);
				app.directive(name, () => {
					return component;
				});
			}
			if (key.endsWith('Service')) {
				//console.log('Registering service', name, key);
				app.service(name, component);
			}
		});
	}

	var p = angular.module("Peach", [
		"angular-loading-bar",
		"chart.js",
		"ngSanitize",
		"ngResource",
		"ngRoute",
		"ngVis",
		"smart-table",
		"treeControl",
		"ui.select", 
		"ui.bootstrap"
	]);

	p.config([
		Constants.Angular.$httpProvider,
		($httpProvider: ng.IHttpProvider) => {
			$httpProvider.interceptors.push(Constants.Services.HttpError);
		}
	]);

	p.factory('PeachConfigResource', [
		Constants.Angular.$resource,
		($resource: ng.resource.IResourceService): IKeyValueResource => {
			return <IKeyValueResource> $resource(
				'/p/conf/wizard/state'
			);
		}
	]);
	p.factory('PitLibraryResource', [
		Constants.Angular.$resource,
		($resource: ng.resource.IResourceService): ILibraryResource => {
			return <ILibraryResource> $resource('/p/libraries');
		}
	]);
	p.factory('PitResource', [
		Constants.Angular.$resource,
		($resource: ng.resource.IResourceService): IPitResource => {
			return <IPitResource> $resource(
				'/p/pits/:id', { id: '@id' }
			);
		}
	]);
	p.factory('PitConfigResource', [
		Constants.Angular.$resource,
		($resource: ng.resource.IResourceService): IPitConfigResource => {
			return <IPitConfigResource> $resource(
				'/p/pits/:id/config', { id: '@id' }
			);
		}
	]);
	p.factory('PitAgentsResource', [
		Constants.Angular.$resource,
		($resource: ng.resource.IResourceService): IPitAgentsResource => {
			return <IPitAgentsResource> $resource(
				'/p/pits/:id/agents', { id: '@id' }
			);
		}
	]);
	p.factory('AvailableMonitorsResource', [
		Constants.Angular.$resource,
		($resource: ng.resource.IResourceService): IMonitorResource => {
			return <IMonitorResource> $resource(
				'/p/conf/wizard/monitors', {}, {
					query: { method: 'GET', isArray: true, cache: true }
				}
			);
		}
	]);
	p.factory('FaultDetailResource', [
		Constants.Angular.$resource,
		($resource: ng.resource.IResourceService): IFaultDetailResource => {
			return <IFaultDetailResource> $resource(
				'/p/faults/:id', { id: '@id' }
			);
		}
	]);

	registerModule(Peach, p);

	p.config([
		Constants.Angular.$routeProvider,
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
				.otherwise({
					redirectTo: "/"
				});
		}
	]);

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

	p.filter('peachParameterName', () => {
		return (value: string): string => {
			return value.substr(0).replace(/[A-Z]/g, ' $&');
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
