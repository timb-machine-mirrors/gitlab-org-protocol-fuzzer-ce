/// <reference path="../reference.ts" />

describe("Peach", () => {
	let C = Peach.C;
	beforeEach(module('Peach'));

	describe('WizardController', () => {
		let $controller: ng.IControllerService;
		let $httpBackend: ng.IHttpBackendService;
		let $rootScope: ng.IRootScopeService;
		let $state: ng.ui.IStateService;

		let ctrl: Peach.WizardController;
		let scope: Peach.IWizardScope;
		let wizardService: Peach.WizardService;

		let WizardController = 'WizardController';
		let pitId = 'PIT_GUID';
		let pitUrl = C.Api.PitUrl.replace(':id', pitId);
		let pit: Peach.IPit = {
			id: pitId,
			name: 'My Pit',
			description: 'description',
			pitUrl: pitUrl,
			tags: [],
			config: [],
			agents: []
		};

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			let pitService: Peach.PitService;
			let $templateCache: ng.ITemplateCacheService;

			$controller = $injector.get(C.Angular.$controller);
			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$rootScope = $injector.get(C.Angular.$rootScope);
			$state = $injector.get(C.Angular.$state);
			$templateCache = $injector.get(C.Angular.$templateCache);

			pitService = $injector.get(C.Services.Pit);
			wizardService = $injector.get(C.Services.Wizard);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Pit.Configure, '');
			$templateCache.put(C.Templates.Pit.Wizard.Intro, '');
			$templateCache.put(C.Templates.Pit.Wizard.Track, '');
			$templateCache.put(C.Templates.Pit.Wizard.Question, '');

			let tracks = [
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
			
			$state.go(C.States.Pit, { pit: pitId });
			$rootScope.$digest();

			$httpBackend.whenGET(pitUrl).respond(pit);
			pitService.LoadPit();
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		function NewCtrl(track) {
			$state.current.data = { track: track };
			scope = <Peach.IWizardScope> $rootScope.$new();
			ctrl = $controller(WizardController, {
				$scope: scope
			});
			$rootScope.$digest();
		}

		function Next() {
			ctrl.Next();
			$rootScope.$digest();
		}

		function NextTrack(track) {
			ctrl.OnNextTrack();
			NewCtrl(track);
		}

		function expectState(state, id?) {
			let actual = $state.is(state, { pit: pitId, id: id });
			expect(actual).toBe(true);
			if (!actual) {
				console.error('expectState', state, id);
				console.error('state is', $state.current.name, JSON.stringify($state.params));
			}
		}

		describe("'vars' track", () => {
			beforeEach(() => {
				$state.go(Peach.WizardTrackIntro(C.Tracks.Vars), { pit: pitId });
			});

			describe("without variables to configure", () => {
				beforeEach(() => {
					NewCtrl(C.Tracks.Vars);
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(scope.Question.id).toBe(0);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
					expectState(Peach.WizardTrackIntro(C.Tracks.Vars));
				});

				it("Next() will move to review", () => {
					$httpBackend.expectPOST(pitUrl, pit).respond(pit);
					Next();
					$httpBackend.flush();
					expectState(Peach.WizardTrackReview(C.Tracks.Vars));
				});

				it("OnNextTrack() moves to the next track", () => {
					NextTrack(C.Tracks.Fault);
					expectState(Peach.WizardTrackIntro(C.Tracks.Fault));
				});
			});

			describe("with variables to configure", () => {
				let key = "Key";
				let name = "Name";
				let value = "Value";

				beforeEach(() => {
					pit.config = [
						{
							key: 'Peach.OS',
							name: 'Peach.OS',
							type: Peach.ParameterType.System,
							value: 'windows'
						},
						{ key: key, name: name, type: Peach.QuestionTypes.String }
					];

					NewCtrl(C.Tracks.Vars);
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(scope.Question.id).toBe(0);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
					expectState(Peach.WizardTrackIntro(C.Tracks.Vars));
				});

				it("should walk thru wizard", () => {
					expectState(Peach.WizardTrackIntro(C.Tracks.Vars));
					$httpBackend.expectGET(pitUrl).respond(pit);
					Next();
					$httpBackend.flush();
					expectState(Peach.WizardTrackSteps(C.Tracks.Vars), 1);

					scope.Question.value = value;
					expect(scope.Question.shortName).toBe(name);
					expect(scope.Question.key).toBe(key);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.String);

					let post = angular.copy(pit);
					post.config = [
						{ key: key, name: name, value: value, type: Peach.QuestionTypes.String }
					];
					$httpBackend.expectPOST(pitUrl, post).respond(post);
					Next();
					$httpBackend.flush();

					expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);
					expectState(Peach.WizardTrackReview(C.Tracks.Vars));
				});
			});
		});

		describe("'fault' track", () => {
			beforeEach(() => {
				$state.go(Peach.WizardTrackIntro(C.Tracks.Fault));

				NewCtrl(C.Tracks.Fault);
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(scope.Question.id).toBe(0);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				expectState(Peach.WizardTrackIntro(C.Tracks.Fault));
			});

			it("should walk thru wizard", () => {
				expectState(Peach.WizardTrackIntro(C.Tracks.Fault));

				$httpBackend.expectGET(pitUrl).respond(pit);
				Next();
				$httpBackend.flush();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1);
				expect(scope.Question.key).toBe("AgentScheme");
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 100);
				expect(scope.Question.id).toBe(100);
				scope.Question.value = 0;
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1100);
				expect(scope.Question.id).toBe(1100);
				scope.Question.value = 0;
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1110);
				expect(scope.Question.id).toBe(1110);
				scope.Question.value = "C:\\some\\program.exe";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1111);
				expect(scope.Question.id).toBe(1111);
				scope.Question.value = "/args";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1140);
				expect(scope.Question.id).toBe(1140);
				scope.Question.value = "StartOnEachIteration";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1141);
				expect(scope.Question.id).toBe(1141);
				scope.Question.value = "";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1142);
				expect(scope.Question.id).toBe(1142);
				scope.Question.value = true;
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Fault), 1143);
				expect(scope.Question.id).toBe(1143);
				scope.Question.value = true;
				Next();

				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected: Peach.IAgent[] = [
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
								"description": "Enable page heap debugging options for an executable."
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
								"description": "Enable Windows debugging."
							}
						]
					}
				];
				
				let actual = wizardService.GetTrack("fault").agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected));
			});
		});

		describe("mocked 'data' track", () => {
			let mockTemplate: Peach.IWizardTemplate = {
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

				$state.go(Peach.WizardTrackIntro(C.Tracks.Data));

				NewCtrl(C.Tracks.Data);
			});

			afterEach(() => {
				// restore the old template for the sake of other unit tests
				wizardService.SetTrackTemplate(C.Tracks.Data, Peach.Wizards.Data);
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(scope.Question.id).toBe(0);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				expectState(Peach.WizardTrackIntro(C.Tracks.Data));
			});

			it("should walk thru wizard", () => {
				expectState(Peach.WizardTrackIntro(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);

				$httpBackend.expectGET(pitUrl).respond(pit);
				Next();
				$httpBackend.flush();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 0;
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 1000);
				expect(scope.Question.id).toBe(1000);
				scope.Question.value = "FooValue";
				Next();

				expectState(Peach.WizardTrackReview(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected: Peach.IAgent[] = [
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

				let actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected));
			});

			it("should allow adding multiple agents", () => {
				expectState(Peach.WizardTrackIntro(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);

				$httpBackend.expectGET(pitUrl).respond(pit);
				Next();
				$httpBackend.flush();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 0;
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 1000);
				expect(scope.Question.id).toBe(1000);
				scope.Question.value = "FooValue";
				Next();

				expectState(Peach.WizardTrackReview(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected1: Peach.IAgent[] = [
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

				let actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected1));

				ctrl.OnRestart();
				$rootScope.$digest();

				expectState(Peach.WizardTrackIntro(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);

				$httpBackend.expectGET(pitUrl).respond(pit);
				Next();
				$httpBackend.flush();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "tcp";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 2);
				expect(scope.Question.id).toBe(2);
				scope.Question.value = "host";
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 1;
				Next();

				expectState(Peach.WizardTrackSteps(C.Tracks.Data), 2000);
				expect(scope.Question.id).toBe(2000);
				scope.Question.value = "BarValue";
				Next();

				expectState(Peach.WizardTrackReview(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected2: Peach.IAgent[] = [
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
