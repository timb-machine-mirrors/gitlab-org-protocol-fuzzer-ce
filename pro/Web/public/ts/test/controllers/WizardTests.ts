/// <reference path="../reference.ts" />

'use strict';

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('WizardController', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $location: ng.ILocationService;

		var newController: () => Peach.WizardController;
		var ctrl: Peach.WizardController;
		var wizardService: Peach.Services.WizardService;
		var pitService: Peach.Services.PitService;

		var pitUrl = '/p/pits/PIT_GUID';
		var pit = {
			name: 'My Pit',
			pitUrl: pitUrl
		}

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $controller: ng.IControllerService;
			var $rootScope: ng.IRootScopeService;

			$httpBackend = $injector.get('$httpBackend');
			$location = $injector.get('$location');
			$rootScope = $injector.get('$rootScope');
			$controller = $injector.get('$controller');
			$httpBackend = $injector.get('$httpBackend');

			$httpBackend.expectGET('/p/libraries').respond([
				{ libraryUrl: '', locked: false }
			]);
			pitService = $injector.get('PitService');
			wizardService = $injector.get('WizardService');

			$httpBackend.expectGET(pitUrl).respond(pit);
			pitService.SelectPit(pitUrl);
			$httpBackend.flush();

			newController = () => {
				return $controller('Peach.WizardController', {
					$scope: $rootScope.$new(),
					$location: $location,
					PitService: pitService,
					WizardService: wizardService
				});
			}
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		describe("'setvars' track", () => {
			describe("without variables to configure", () => {
				beforeEach(() => {
					$location.path("/quickstart/setvars");
					$httpBackend.expectGET(pitUrl + '/config').respond(
						{
							pitUrl: pitUrl,
							config: []
						}
					);
					ctrl = newController();
					$httpBackend.flush();
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(ctrl.Question.id).toBe(0);
					expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
				});

				it("Next() will move to done", () => {
					$httpBackend.expectPOST(pitUrl + "/config").respond('OK');
					ctrl.Next();
					$httpBackend.flush();
					expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Done);
				});

				it("OnSubmit() moves to the next wizard", () => {
					ctrl.OnNextTrack();
					expect($location.path()).toBe("/quickstart/fault");
				});
			});

			describe("with variables to configure", () => {
				beforeEach(() => {
					$location.path("/quickstart/setvars");
					$httpBackend.expectGET(pitUrl + '/config').respond(
						{
							pitUrl: pitUrl,
							config: [
								{ key: "Key", name: "Name", type: Peach.Models.QuestionTypes.String }
							]
						}
					);
					ctrl = newController();
					$httpBackend.flush();
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(ctrl.Question.id).toBe(0);
					expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
				});

				it("should walk thru wizard", () => {
					ctrl.Next();
					ctrl.Question.value = "Value";
					expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.String);
					var data = <Peach.Models.IPitConfig> {
						pitUrl: pitUrl,
						config: [
							{ key: "Key", name: "Name", value: "Value" }
						]
					};
					$httpBackend.expectPOST(pitUrl + "/config", data).respond('OK');
					ctrl.Next();
					$httpBackend.flush();
					expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Done);
				});
			});
		});

		describe("'fault' track", () => {
			beforeEach(() => {
				$location.path("/quickstart/fault");
				$httpBackend.expectGET('/p/conf/wizard/state').respond([
					{ key: "LocalOS", value: "windows" }
				]);
				$httpBackend.expectGET(pitUrl + '/config').respond(
					{ pitUrl: pitUrl, config: [] }
				);
				ctrl = newController();
				$httpBackend.flush();
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(ctrl.Question.id).toBe(0);
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
			});

			it("should walk thru wizard", () => {
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
				ctrl.Next();

				expect(ctrl.Question.key).toBe("AgentScheme");
				expect(ctrl.Question.id).toBe(1);
				ctrl.Question.value = "local";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(100);
				ctrl.Question.value = 0;
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1100);
				ctrl.Question.value = 0;
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1110);
				ctrl.Question.value = "C:\\some\\program.exe";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1111);
				ctrl.Question.value = "/args";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1140);
				ctrl.Question.value = "StartOnEachIteration";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1141);
				ctrl.Question.value = "";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1142);
				ctrl.Question.value = true;
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1143);
				ctrl.Question.value = true;

				var data = <Peach.Models.IPitAgents> {
					pitUrl: pitUrl,
					"agents": [
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
					]
				};
				$httpBackend.expectPOST(pitUrl + "/agents", data).respond(data);
				ctrl.Next();
				$httpBackend.flush();
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Done);
			});
		});

		describe("mocked 'data' track", () => {
			var mockTemplate: Peach.Models.IWizardTemplate = {
				qa: [
					{
						id: 0,
						type: "intro",
						qref: "html/wizard/data/intro.html",
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
				wizardService.SetTrackTemplate("data", mockTemplate);
				$location.path("/quickstart/data");
				$httpBackend.expectGET('/p/conf/wizard/state').respond([
					{ key: "LocalOS", value: "windows" }
				]);
				$httpBackend.expectGET(pitUrl + '/config').respond(
					{ pitUrl: pitUrl, config: [] }
				);
				ctrl = newController();
				$httpBackend.flush();
			});

			afterEach(() => {
				// restore the old template for the sake of other unit tests
				wizardService.SetTrackTemplate("data", Peach.Wizards.Data);
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(ctrl.Question.id).toBe(0);
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
			});

			it("should walk thru wizard", () => {
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1);
				ctrl.Question.value = "local";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(3);
				ctrl.Question.value = 0;
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1000);
				ctrl.Question.value = "FooValue";

				var data = <Peach.Models.IPitAgents> {
					pitUrl: pitUrl,
					"agents": [
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
					]
				};
				$httpBackend.expectPOST(pitUrl + "/agents", data).respond(data);
				ctrl.Next();
				$httpBackend.flush();
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Done);
			});

			it("should allow adding multiple agents", () => {
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1);
				ctrl.Question.value = "local";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(3);
				ctrl.Question.value = 0;
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1000);
				ctrl.Question.value = "FooValue";

				var data1 = <Peach.Models.IPitAgents> {
					pitUrl: pitUrl,
					"agents": [
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
					]
				};
				$httpBackend.expectPOST(pitUrl + "/agents", data1).respond(data1);
				ctrl.Next();
				$httpBackend.flush();
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Done);

				ctrl.OnRestart();
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Intro);
				ctrl.Next();

				expect(ctrl.Question.id).toBe(1);
				ctrl.Question.value = "tcp";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(2);
				ctrl.Question.value = "host";
				ctrl.Next();

				expect(ctrl.Question.id).toBe(3);
				ctrl.Question.value = 1;
				ctrl.Next();

				expect(ctrl.Question.id).toBe(2000);
				ctrl.Question.value = "BarValue";

				var data2 = <Peach.Models.IPitAgents> {
					pitUrl: pitUrl,
					"agents": [
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
					]
				};
				$httpBackend.expectPOST(pitUrl + "/agents", data2).respond(data2);
				ctrl.Next();
				$httpBackend.flush();
				expect(ctrl.Question.type).toBe(Peach.Models.QuestionTypes.Done);
			});
		});
	});
});
