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
				app.controller(name, component);
			}
			if (key.endsWith('Directive')) {
				app.directive(name, () => {
					return component;
				});
			}
			if (key.endsWith('Service')) {
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
			$stateProvider: angular.ui.IStateProvider,
			$urlRouterProvider: ng.ui.IUrlRouterProvider
		) => {
			$urlRouterProvider.otherwise('/');

			$stateProvider
				// ----- Main -----
				.state(C.States.Main, { 
					abstract: true,
					template: '<div ui-view></div>'
				})
				.state(C.States.MainHome, {
					url: '/',
					templateUrl: C.Templates.Home,
					controller: HomeController,
					controllerAs: 'vm'
				})
				.state(C.States.MainLibrary, {
					url: '/library',
					templateUrl: C.Templates.Library,
					controller: LibraryController,
					controllerAs: 'vm'
				})
				.state(C.States.MainTemplates, {
					url: '/templates',
					templateUrl: C.Templates.Templates,
					controller: TemplatesController,
					controllerAs: 'vm'
				})
				.state(C.States.MainJobs, {
					url: '/jobs',
					templateUrl: C.Templates.Jobs,
					controller: JobsController,
					controllerAs: 'vm'
				})

				// ----- Job -----
				.state(C.States.Job, {
					url: '/job/:job',
					abstract: true
				})
				.state(C.States.JobDashboard, {
					url: '/',
					views: {
						'@': {
							templateUrl: C.Templates.Job,
							controller: DashboardController,
							controllerAs: 'vm'
						}
					}
				})
				.state(C.States.JobFaults, {
					url: '/faults/:bucket',
					params: { bucket: 'all' },
					views: {
						'@': {
							templateUrl: C.Templates.Faults,
							controller: FaultsController,
							controllerAs: 'vm'
						}
					}
				})
				.state(C.States.JobFaultsDetail, {
					url: '/{id:int}',
					views: {
						'@': {
							templateUrl: C.Templates.FaultsDetail,
							controller: FaultsDetailController,
							controllerAs: 'vm'
						}
					}
				})
				.state(C.States.JobMetrics, {
					url: '/metrics/:metric',
					views: {
						'@': {
							templateUrl: params =>
								C.Templates.MetricPage.replace(':metric', params.metric),
							controller: MetricsController,
							controllerAs: 'vm'
						}
					}
				})

				// ----- Configure -----
				.state(C.States.Pit, {
					url: '/pit/:pit',
					abstract: true
				})
				.state(C.States.PitWizard, {
					url: '/quickstart/:track/{id:int}',
					templateUrl: params => {
						switch (params.track) {
							case C.Tracks.Intro:
								return C.Templates.Wizard.Intro;
							case C.Tracks.Test:
								return C.Templates.Wizard.Test;
						}
						return C.Templates.Wizard.Track;
					},
					controllerProvider: ($stateParams: any): any => {
						switch ($stateParams.track) {
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
				.state(C.States.PitWizardIntro, {
					url: '/intro',
					templateUrl: params =>
						C.Templates.Wizard.TrackIntro.replace(':track', params.track)
				})
				.state(C.States.PitWizardQuestion, {
					templateUrl: C.Templates.Wizard.Question,
					controller: WizardQuestionController,
					controllerAs: 'vm'
				})
				.state(C.States.PitWizardReview, {
					url: '/review',
					templateUrl: params =>
						C.Templates.Wizard.TrackDone.replace(':track', params.track)
				})
				.state(C.States.PitConfig, {
					abstract: true,
					url: '/config',
					template: '<div ui-view></div>'
				})
				.state(C.States.PitConfigVariables, {
					url: '/variables',
					templateUrl: C.Templates.Config.Variables,
					controller: ConfigureVariablesController,
					controllerAs: 'vm'
				})
				.state(C.States.PitConfigMonitoring, {
					url: '/monitoring',
					templateUrl: C.Templates.Config.Monitoring,
					controller: ConfigureMonitorsController,
					controllerAs: 'vm'
				})
				.state(C.States.PitConfigTest, {
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
