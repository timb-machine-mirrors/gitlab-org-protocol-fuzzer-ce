﻿/// <reference path="reference.ts" />

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
		"ngMessages",
		"angular-loading-bar",
		"chart.js",
		"ncy-angular-breadcrumb",
		"ngSanitize",
		"ngVis",
		"smart-table",
		"ui.bootstrap",
		"ui.select",
		"ui.router"
	]);

	registerModule(Peach, p);

	p.config([
		C.Angular.$breadcrumbProvider,
		($breadcrumbProvider) => {
			$breadcrumbProvider.setOptions({
				prefixStateName: C.States.MainHome,
				includeAbstract: true
			});
		}
	]);

	p.config([
		C.Angular.$stateProvider,
		C.Angular.$urlRouterProvider, (
			$stateProvider: angular.ui.IStateProvider,
			$urlRouterProvider: ng.ui.IUrlRouterProvider
		) => {
			$urlRouterProvider.otherwise('/error');

			$stateProvider
				// ----- Main -----
				.state(C.States.Main, { 
					abstract: true,
					template: C.Templates.UiView,
					ncyBreadcrumb: { skip: true }
				})
				.state(C.States.MainHome, {
					url: '/',
					templateUrl: C.Templates.Home,
					controller: HomeController,
					controllerAs: C.ViewModel,
					ncyBreadcrumb: { label: 'Home' }
				})
				.state(C.States.MainLibrary, {
					url: '/library',
					templateUrl: C.Templates.Library,
					controller: LibraryController,
					controllerAs: C.ViewModel,
					ncyBreadcrumb: { label: 'Library' }
				})
				.state(C.States.MainJobs, {
					url: '/jobs',
					templateUrl: C.Templates.Jobs,
					controller: JobsController,
					controllerAs: C.ViewModel,
					ncyBreadcrumb: { label: 'Jobs' }
				})
				.state(C.States.MainError, {
					url: '/error',
					templateUrl: C.Templates.Error,
					controller: ErrorController,
					controllerAs: C.ViewModel,
					params: { message: undefined },
					ncyBreadcrumb: { label: 'Oops!' }
				})

				// ----- Job -----
				.state(C.States.Job, {
					url: '/job/:job',
					templateUrl: C.Templates.Job.Dashboard,
					controller: DashboardController,
					controllerAs: C.ViewModel,
					ncyBreadcrumb: { 
						label: '{{job.name}}',
						parent: C.States.MainJobs
					},
					onEnter: [
						C.Services.Job, 
						C.Angular.$stateParams, 
					(
						jobService: JobService,
						$stateParams: any
					) => {
						jobService.OnEnter($stateParams.job);
					}],
					onExit: [C.Services.Job, (jobService: JobService) => {
						jobService.OnExit();
					}]
				})
				.state(C.States.JobFaults, {
					url: '/faults/:bucket',
					params: { bucket: 'all' },
					views: {
						'@': {
							templateUrl: C.Templates.Job.Faults.Summary,
							controller: FaultsController,
							controllerAs: C.ViewModel
						}
					},
					ncyBreadcrumb: { label: '{{FaultSummaryTitle}}' }
				})
				.state(C.States.JobFaultsDetail, {
					url: '/{id:int}',
					views: {
						'@': {
							templateUrl: C.Templates.Job.Faults.Detail,
							controller: FaultsDetailController,
							controllerAs: C.ViewModel
						}
					},
					ncyBreadcrumb: { label: '{{FaultDetailTitle}}' }
				})
				.state(C.States.JobMetrics, {
					url: '/metrics',
					abstract: true,
					ncyBreadcrumb: { label: 'Metrics' }
				})

				// ----- Pit -----
				.state(C.States.Pit, {
					url: '/pit/:pit',
					templateUrl: C.Templates.Pit.Configure,
					controller: ConfigureController,
					controllerAs: C.ViewModel,
					params: {
						seed: undefined,
						rangeStart: undefined,
						rangeStop: undefined
					},
					ncyBreadcrumb: { 
						label: '{{pit.name}}',
						parent: C.States.MainLibrary
					}
				})
				.state(C.States.PitWizard, {
					url: '/quickstart',
					views: {
						'@': {
							templateUrl: C.Templates.Pit.Wizard.Intro,
							controller: WizardController,
							controllerAs: C.ViewModel
						}
					},
					data: { track: C.Tracks.Intro },
					ncyBreadcrumb: { label: 'Quick Start' }
				})
				.state(C.States.PitWizardTest, {
					url: '/test',
					views: {
						'@': {
							templateUrl: C.Templates.Pit.Wizard.Test,
							controller: PitTestController,
							controllerAs: C.ViewModel
						}
					},
					ncyBreadcrumb: { label: 'Test' }
				})
				.state(C.States.PitAdvanced, {
					abstract: true,
					url: '/advanced',
					ncyBreadcrumb: { label: 'Configure' }
				})
				.state(C.States.PitAdvancedVariables, {
					url: '/variables',
					views: {
						'@': {
							templateUrl: C.Templates.Pit.Advanced.Variables,
							controller: ConfigureVariablesController,
							controllerAs: C.ViewModel
						}
					},
					ncyBreadcrumb: { label: 'Variables' }
				})
				.state(C.States.PitAdvancedMonitoring, {
					url: '/monitoring',
					views: {
						'@': {
							templateUrl: C.Templates.Pit.Advanced.Monitoring,
							controller: ConfigureMonitorsController,
							controllerAs: C.ViewModel
						}
					},
					ncyBreadcrumb: { label: 'Monitoring' }
				})
				.state(C.States.PitAdvancedTest, {
					url: '/test',
					views: {
						'@': {
							templateUrl: C.Templates.Pit.Advanced.Test,
							controller: PitTestController,
							controllerAs: C.ViewModel
						}
					},
					ncyBreadcrumb: { label: 'Test' }
				})
			;

			_.forEach(C.MetricsList,(metric: C.IMetric) => {
				var state = [C.States.JobMetrics, metric.id].join('.');
				$stateProvider.state(state, {
					url: '/' + metric.id,
					views: {
						'@': {
							templateUrl: C.Templates.Job.MetricPage.replace(':metric', metric.id),
							controller: MetricsController,
							controllerAs: C.ViewModel
						}
					},
					params: { metric: metric.id },
					ncyBreadcrumb: { label: metric.name }
				});
			});

			_.forEach(WizardTracks, (track: ITrackStatic) => {
				if (track.skip)
					return;

				var views = {
					'@': {
						templateUrl: C.Templates.Pit.Wizard.Track,
						controller: WizardController,
						controllerAs: C.ViewModel
					}
				};

				views['@pit.wizard.' + track.id] = {
					templateUrl: C.Templates.Pit.Wizard.TrackIntro.replace(':track', track.id)
				};

				$stateProvider
					.state([C.States.PitWizard, track.id].join('.'), {
						url: '/' + track.id,
						views: views,
						data: { track: track.id },
						ncyBreadcrumb: { label: track.name }
					})
					.state([C.States.PitWizard, track.id, 'review'].join('.'), {
						url: '/review',
						templateUrl: C.Templates.Pit.Wizard.TrackDone.replace(':track', track.id),
						ncyBreadcrumb: { label: 'Review' }
					})
					.state([C.States.PitWizard, track.id, 'steps'].join('.'), {
						url: '/{id:int}',
						templateUrl: C.Templates.Pit.Wizard.Question,
						controller: WizardQuestionController,
						controllerAs: C.ViewModel,
						ncyBreadcrumb: { label: 'Questions & Answers' }
					})
				;
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
