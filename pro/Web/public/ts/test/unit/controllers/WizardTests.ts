/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('WizardController', () => {
		var $controller: ng.IControllerService;
		var $httpBackend: ng.IHttpBackendService;
		var $rootScope: ng.IRootScopeService;
		var $state: ng.ui.IStateService;

		var ctrl: Peach.WizardController;
		var scope: Peach.IWizardScope;
		var wizardService: Peach.WizardService;

		var WizardController = 'WizardController';
		var pitUrl = '/p/pits/PIT_GUID';
		var pit = {
			name: 'My Pit',
			pitUrl: pitUrl,
			peachConfig: [{ key: "LocalOS", value: "windows" }],
			config: []
		};

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var pitService: Peach.PitService;
			var $templateCache: ng.ITemplateCacheService;

			$controller = $injector.get(C.Angular.$controller);
			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$rootScope = $injector.get(C.Angular.$rootScope);
			$state = $injector.get(C.Angular.$state);
			$templateCache = $injector.get(C.Angular.$templateCache);

			pitService = $injector.get(C.Services.Pit);
			wizardService = $injector.get(C.Services.Wizard);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Pit.Wizard.Track, '');
			$templateCache.put(C.Templates.Pit.Wizard.Question, '');

			var tracks = [
				C.Tracks.Vars,
				C.Tracks.Fault,
				C.Tracks.Data
			];
			tracks.forEach(track => {
				$templateCache.put(
					C.Templates.Pit.Wizard.TrackIntro.replace(':track', track), ''
				);
				$templateCache.put(
					C.Templates.Pit.Wizard.TrackDone.replace(':track', track), ''
				);
			});

			$httpBackend.expectGET(pitUrl).respond(pit);
			pitService.SelectPit(pitUrl);
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		function NewCtrl() {
			scope = <Peach.IWizardScope> $rootScope.$new();
			ctrl = $controller(WizardController, {
				$scope: scope,
				WizardService: wizardService
			});
			$rootScope.$digest();
		}

		function Next() {
			ctrl.Next();
			$rootScope.$digest();
			NewCtrl();
		}

		function NextTrack() {
			ctrl.OnNextTrack();
			$rootScope.$digest();
			NewCtrl();
		}

		function expectState(state, track, id?) {
			var actual = $state.is(state, { track: track, id: id });
			expect(actual).toBe(true);
			if (!actual) {
				console.error('expectState', state, track, id);
				console.error('state is', $state.current.name, JSON.stringify($state.params));
			}
		}

		describe("'vars' track", () => {
			beforeEach(() => {
				$state.go(C.States.PitWizardIntro, { track: C.Tracks.Vars });
				$rootScope.$digest();
			});

			describe("without variables to configure", () => {
				beforeEach(() => {
					$httpBackend.expectGET(pitUrl).respond(pit);
					NewCtrl();
					$httpBackend.flush();
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(scope.Question.id).toBe(0);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
					expectState(C.States.PitWizardIntro, C.Tracks.Vars);
				});

				it("Next() will move to review", () => {
					$httpBackend.expectPOST(pitUrl, pit).respond(pit);
					Next();
					$httpBackend.flush();
					expectState(C.States.PitWizardReview, C.Tracks.Vars);
				});

				it("OnNextTrack() moves to the next track", () => {
					$httpBackend.expectGET(pitUrl).respond(pit);
					NextTrack();
					$httpBackend.flush();
					expectState(C.States.PitWizardIntro, C.Tracks.Fault);
				});
			});

			describe("with variables to configure", () => {
				beforeEach(() => {
					pit.config = [
						{ key: "Key", name: "Name", type: Peach.QuestionTypes.String }
					];

					$httpBackend.expectGET(pitUrl).respond(pit);
					NewCtrl();
					$httpBackend.flush();
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(scope.Question.id).toBe(0);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
					expectState(C.States.PitWizardIntro, C.Tracks.Vars);
				});

				it("should walk thru wizard", () => {
					expectState(C.States.PitWizardIntro, C.Tracks.Vars);
					Next();
					expectState(C.States.PitWizardQuestion, C.Tracks.Vars, 1);

					scope.Question.value = "Value";
					expect(scope.Question.type).toBe(Peach.QuestionTypes.String);

					var post = angular.copy(pit);
					post.config = [
						{ key: "Key", name: "Name", value: "Value", type: "string" }
					];
					$httpBackend.expectPOST(pitUrl, post).respond(post);
					Next();
					$httpBackend.flush();

					expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);
					expectState(C.States.PitWizardReview, C.Tracks.Vars);
				});
			});
		});

		describe("'fault' track", () => {
			beforeEach(() => {
				$state.go(C.States.PitWizardIntro, { track: C.Tracks.Fault });
				$rootScope.$digest();

				$httpBackend.expectGET(pitUrl).respond(pit);
				NewCtrl();
				$httpBackend.flush();
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(scope.Question.id).toBe(0);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				expectState(C.States.PitWizardIntro, C.Tracks.Fault);
			});

			it("should walk thru wizard", () => {
				expectState(C.States.PitWizardIntro, C.Tracks.Fault);
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1);
				expect(scope.Question.key).toBe("AgentScheme");
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				// Jump occurred
				NewCtrl();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 100);
				expect(scope.Question.id).toBe(100);
				scope.Question.value = 0;
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1100);
				expect(scope.Question.id).toBe(1100);
				scope.Question.value = 0;
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1110);
				expect(scope.Question.id).toBe(1110);
				scope.Question.value = "C:\\some\\program.exe";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1111);
				expect(scope.Question.id).toBe(1111);
				scope.Question.value = "/args";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1140);
				expect(scope.Question.id).toBe(1140);
				scope.Question.value = "StartOnEachIteration";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1141);
				expect(scope.Question.id).toBe(1141);
				scope.Question.value = "";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1142);
				expect(scope.Question.id).toBe(1142);
				scope.Question.value = true;
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Fault, 1143);
				expect(scope.Question.id).toBe(1143);
				scope.Question.value = true;
				Next();

				//expectState(C.States.WizardReview, C.Tracks.Fault);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				var expected: Peach.Agent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "PageHeap",
								"path": [1110],
								"map": [
									{ "name": "Executable", "value": "C:\\some\\program.exe" },
									{ "name": "WinDbgPath", "value": "" }
								],
								"description": "Enable page heap debugging options for an executable. "
							}, {
								"monitorClass": "WindowsDebugger",
								"path": [1100],
								"map": [
									{ "name": "Executable", "value": "C:\\some\\program.exe" },
									{ "name": "Arguments", "value": "/args" },
									{ "name": "ProcessName" },
									{ "name": "Service" },
									{ "name": "WinDbgPath", "value": "" },
									{ "name": "StartMode", "value": "StartOnEachIteration" },
									{ "name": "IgnoreFirstChanceGuardPage", "value": true }
								],
								"description": "Enable Windows debugging. "
							}
						]
					}
				];
				
				var actual = wizardService.GetTrack("fault").agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected));
			});
		});

		describe("mocked 'data' track", () => {
			var mockTemplate: Peach.IWizardTemplate = {
				qa: [
					{
						id: 0,
						type: "intro",
						next: 1
					},
					{
						id: 1,
						type: "choice",
						key: "AgentScheme",
						choice: [
							{ value: "local", next: 3 },
							{ value: "tcp", next: 2 }
						]
					},
					{
						id: 2,
						type: "string",
						key: "AgentHost",
						next: 3
					},
					{
						id: 3,
						type: "choice",
						choice: [
							{ value: 0, next: 1000 },
							{ value: 1, next: 2000 }
						]
					},
					{
						id: 1000,
						type: "string",
						key: "FooParam"
					},
					{
						id: 2000,
						type: "string",
						key: "BarParam"
					}
				],
				monitors: [
					{
						monitorClass: "FooMonitor",
						path: [1000],
						map: [
							{ key: "FooParam", name: "Param" }
						],
						description: "Foo monitor."
					},
					{
						monitorClass: "BarMonitor",
						path: [2000],
						map: [
							{ key: "BarParam", name: "Param" }
						],
						description: "Bar monitor."
					}
				]
			};

			beforeEach(() => {
				wizardService.SetTrackTemplate(C.Tracks.Data, mockTemplate);

				$state.go(C.States.PitWizardIntro, { track: C.Tracks.Data });
				$rootScope.$digest();

				$httpBackend.expectGET(pitUrl).respond(pit);
				NewCtrl();
				$httpBackend.flush();
			});

			afterEach(() => {
				// restore the old template for the sake of other unit tests
				wizardService.SetTrackTemplate(C.Tracks.Data, Peach.Wizards.Data);
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(scope.Question.id).toBe(0);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				expectState(C.States.PitWizardIntro, C.Tracks.Data);
			});

			it("should walk thru wizard", () => {
				expectState(C.States.PitWizardIntro, C.Tracks.Data);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 0;
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 1000);
				expect(scope.Question.id).toBe(1000);
				scope.Question.value = "FooValue";
				Next();

				expectState(C.States.PitWizardReview, C.Tracks.Data);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				var expected: Peach.Agent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "FooMonitor",
								"path": [1000],
								"map": [
									{ "name": "Param", "value": "FooValue" }
								],
								"description": "Foo monitor."
							}
						]
					}
				];

				var actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected));
			});

			it("should allow adding multiple agents", () => {
				expectState(C.States.PitWizardIntro, C.Tracks.Data);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 0;
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 1000);
				expect(scope.Question.id).toBe(1000);
				scope.Question.value = "FooValue";
				Next();

				expectState(C.States.PitWizardReview, C.Tracks.Data);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				var expected1: Peach.Agent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "FooMonitor",
								"path": [1000],
								"map": [
									{ "name": "Param", "value": "FooValue" }
								],
								"description": "Foo monitor."
							}
						]
					}
				];

				var actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected1));

				ctrl.OnRestart();
				$rootScope.$digest();

				$httpBackend.expectGET(pitUrl).respond(pit);
				NewCtrl();
				$httpBackend.flush();

				expectState(C.States.PitWizardIntro, C.Tracks.Data);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "tcp";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 2);
				expect(scope.Question.id).toBe(2);
				scope.Question.value = "host";
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 1;
				Next();

				expectState(C.States.PitWizardQuestion, C.Tracks.Data, 2000);
				expect(scope.Question.id).toBe(2000);
				scope.Question.value = "BarValue";
				Next();

				expectState(C.States.PitWizardReview, C.Tracks.Data);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				var expected2: Peach.Agent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "FooMonitor",
								"path": [1000],
								"map": [
									{ "name": "Param", "value": "FooValue" }
								],
								"description": "Foo monitor."
							}
						]
					},
					{
						"name": "",
						"agentUrl": "tcp://host",
						"monitors": [
							{
								"monitorClass": "BarMonitor",
								"path": [2000],
								"map": [
									{ "name": "Param", "value": "BarValue" }
								],
								"description": "Bar monitor."
							}
						]
					}
				];

				actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected2));
			});
		});
	});
});
