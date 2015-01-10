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

	registerModule(Peach, p);

	p.config([
		Constants.Angular.$routeProvider,
		($routeProvider: ng.route.IRouteProvider) => {
			$routeProvider
				.when(Constants.Routes.Home, {
					templateUrl: Constants.Templates.Dashboard,
					controller: DashboardController
				})
				.when(Constants.Routes.Faults, {
					templateUrl: Constants.Templates.Faults,
					controller: FaultsController
				})
				.when(Constants.Routes.Metrics, {
					templateUrl: Constants.Templates.Metrics,
					controller: MetricsController
				})
				.when(Constants.Routes.WizardPrefix + Constants.Tracks.Intro, {
					templateUrl: Constants.Templates.Wizard.Intro,
					controller: WizardController
				})
				.when(Constants.Routes.WizardPrefix + Constants.Tracks.Done, {
					templateUrl: Constants.Templates.Wizard.Done,
					controller: WizardController
				})
				.when(Constants.Routes.WizardPrefix + Constants.Tracks.Test, {
					templateUrl: Constants.Templates.Wizard.Test,
					controller: PitTestController
				})
				.when(Constants.Routes.WizardStep, {
					templateUrl: Constants.Templates.Wizard.Step,
					controller: WizardController
				})
				.when(Constants.Routes.ConfigMonitoring, {
					templateUrl: Constants.Templates.Config.Monitoring,
					controller: ConfigureMonitorsController
				})
				.when(Constants.Routes.ConfigVariables, {
					templateUrl: Constants.Templates.Config.Variables,
					controller: ConfigureVariablesController
				})
				.when(Constants.Routes.ConfigTest, {
					templateUrl: Constants.Templates.Config.Test,
					controller: PitTestController
				})
				.otherwise({
					redirectTo: Constants.Routes.Home
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
