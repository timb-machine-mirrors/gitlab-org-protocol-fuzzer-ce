/// <reference path="reference.ts" />

module Peach {
	"use strict";

	var p = angular.module("Peach", [
		"angles",
		"angular-loading-bar",
		"ngSanitize",
		"ngResource",
		"ngRoute",
		"ngVis",
		"smart-table",
		"treeControl",
		"ui.select", 
		"ui.bootstrap"
	]);

	p.service('HttpErrorService', HttpErrorService);
	p.config([
		'$httpProvider', ($httpProvider: ng.IHttpProvider) => {
			$httpProvider.interceptors.push('HttpErrorService');
		}
	]);

	p.factory('PeachConfigResource', [
		'$resource',
		($resource: ng.resource.IResourceService): IKeyValueResource => {
			return <IKeyValueResource> $resource(
				'/p/conf/wizard/state'
			);
		}
	]);
	p.factory('PitLibraryResource', [
		'$resource',
		($resource: ng.resource.IResourceService): ILibraryResource => {
			return <ILibraryResource> $resource('/p/libraries');
		}
	]);
	p.factory('PitResource', [
		'$resource',
		($resource: ng.resource.IResourceService): IPitResource => {
			return <IPitResource> $resource(
				'/p/pits/:id', { id: '@id' }
			);
		}
	]);
	p.factory('PitConfigResource', [
		'$resource',
		($resource: ng.resource.IResourceService): IPitConfigResource => {
			return <IPitConfigResource> $resource(
				'/p/pits/:id/config', { id: '@id' }
			);
		}
	]);
	p.factory('PitAgentsResource', [
		'$resource',
		($resource: ng.resource.IResourceService): IPitAgentsResource => {
			return <IPitAgentsResource> $resource(
				'/p/pits/:id/agents', { id: '@id' }
			);
		}
	]);
	p.factory('AvailableMonitorsResource', [
		'$resource',
		($resource: ng.resource.IResourceService): IMonitorResource => {
			return <IMonitorResource> $resource(
				'/p/conf/wizard/monitors', {}, {
					query: { method: 'GET', isArray: true, cache: true }
				}
			);
		}
	]);
	p.factory('FaultDetailResource', [
		'$resource',
		($resource: ng.resource.IResourceService): IFaultDetailResource => {
			return <IFaultDetailResource> $resource(
				'/p/faults/:id', { id: '@id' }
			);
		}
	]);

	p.service('PitService', PitService);
	p.service('JobService', JobService);
	p.service('TestService', TestService);
	p.service('WizardService', WizardService);
	p.service('UniqueService', UniqueService);

	p.directive("integer", () => new IntegerDirective());
	p.directive("hexstring", () => new HexDirective());
	p.directive('peachRange', () => new RangeDirective());

	p.directive('peachAgent', () => new AgentDirective());
	p.directive('peachMonitor', () => new MonitorDirective());
	p.directive('peachQuestion', () => new QuestionDirective());
	p.directive('peachParameter', () => new ParameterDirective());
	p.directive('peachParameterInput', () => new ParameterInputDirective());
	p.directive('peachTest', () => new TestDirective());
	p.directive('peachUnsaved', [
		'$modal', '$location', (
			$modal: ng.ui.bootstrap.IModalService,
			$location: ng.ILocationService
		) => new UnsavedDirective($modal, $location)
	]);
	p.directive('peachUnique', () => new UniqueDirective());
	p.directive('peachUniqueChannel', [
		'UniqueService',
		(service: UniqueService) => new UniqueChannelDirective(service)
	]);
	p.directive('peachCombobox', [
		'$document', (
			$document: ng.IDocumentService
		) => new ComboboxDirective($document)
	]);

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
