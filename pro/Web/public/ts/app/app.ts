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
		"ngMessages",
		"ngSanitize",
		"ngVis",
		"smart-table",
		"treeControl",
		"ui.bootstrap",
		"ui.select",
		"ui.router"
	]);

	registerModule(Peach, p);

	p.config([
		C.Angular.$httpProvider,
		($httpProvider: ng.IHttpProvider) => {
			$httpProvider.interceptors.push(C.Services.HttpError);
		}
	]);

	p.config([
		C.Angular.$stateProvider,
		C.Angular.$urlRouterProvider, (
			$stateProvider: ng.ui.IStateProvider,
			$urlRouterProvider: ng.ui.IUrlRouterProvider
		) => {
			$urlRouterProvider.otherwise('/');

			$stateProvider
				.state(C.States.Home, {
					url: '/',
					templateUrl: C.Templates.Dashboard,
					controller: DashboardController,
					controllerAs: 'vm'
				})
				.state(C.States.Faults, {
					url: '/faults',
					templateUrl: C.Templates.Faults,
					controller: FaultsController,
					controllerAs: 'vm'
				})
				.state(C.States.Metrics, {
					url: '/metrics/:metric',
					templateUrl: params =>
						C.Templates.MetricPage.replace(':metric', params.metric),
					controller: MetricsController,
					controllerAs: 'vm'
				})
				.state(C.States.Wizard, {
					abstract: true,
					url: '/quickstart/:track',
					templateUrl: C.Templates.Wizard.Base,
					controller: WizardBaseController,
					controllerAs: 'vm'
				})
				.state(C.States.WizardTrack, {
					url: '/{id:int}',
					templateUrl: params => {
						switch (params.track) {
							case C.Tracks.Intro:
								return C.Templates.Wizard.Intro;
							case C.Tracks.Test:
								return C.Templates.Wizard.Test;
						}
						return C.Templates.Wizard.Track;
					},
					controllerProvider: ($stateParams): any => {
						switch ($stateParams.track) {
							case C.Tracks.Intro:
								return WizardBaseController;
							case C.Tracks.Test:
								return PitTestController;
						}
						return WizardController;
					},
					controllerAs: 'vm',
					params: {
						id: { value: 0, squash: true }
					}
				})
				.state(C.States.WizardTrackIntro, {
					url: '/intro',
					templateUrl: params =>
						C.Templates.Wizard.TrackIntro.replace(':track', params.track)
				})
				.state(C.States.WizardTrackQuestion, {
					templateUrl: C.Templates.Wizard.Question,
					controller: WizardTrackQuestionController,
					controllerAs: 'vm'
				})
				.state(C.States.WizardTrackReview, {
					url: '/review',
					templateUrl: params =>
						C.Templates.Wizard.TrackDone.replace(':track', params.track)
				})
				.state(C.States.Config, {
					abstract: true,
					url: '/config',
					template: '<div ui-view></div>'
				})
				.state(C.States.ConfigVariables, {
					url: '/variables',
					templateUrl: C.Templates.Config.Variables,
					controller: ConfigureVariablesController,
					controllerAs: 'vm'
				})
				.state(C.States.ConfigMonitoring, {
					url: '/monitoring',
					templateUrl: C.Templates.Config.Monitoring,
					controller: ConfigureMonitorsController,
					controllerAs: 'vm'
				})
				.state(C.States.ConfigTest, {
					url: '/test',
					templateUrl: C.Templates.Config.Test,
					controller: PitTestController,
					controllerAs: 'vm'
				})
			;
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
