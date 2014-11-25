﻿/// <reference path="../includes.d.ts" />

module DashApp {
	"use strict";

	declare function ngGridFlexibleHeightPlugin(opts?: any): void;

	export interface IWizardParams extends ng.route.IRouteParamsService {
		step: string;
	}

	export class WizardController {
		//#region private variables
		private params: IWizardParams;
		private questionPath: number[] = [0];

		private location: ng.ILocationService;
		private peach: Services.IPeachService;
		private pitConfigSvc: Services.IPitConfiguratorService;
		//#endregion

		//#region ctor
		static $inject = ["$scope", "$routeParams", "$location", "peachService", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, $routeParams: IWizardParams, $location: ng.ILocationService, peachService: Services.IPeachService, pitConfiguratorService: Services.IPitConfiguratorService) {
			this.params = $routeParams;

			$scope.vm = this;
			this.location = $location;
			this.peach = peachService;
			this.pitConfigSvc = pitConfiguratorService;

			if (this.params.step != undefined)
				this.refreshData($scope);
		}
		//#endregion

		//#region Public Properties
		public isDefaultsOpen: boolean = false;

		public get qa(): Models.Question[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.QA;
			else
				return undefined;
		}

		public get monitors(): Models.Monitor[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.Monitors;
			else
				return undefined;
		}

		public get defines(): Models.PitConfig {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.Defines;
			else
				return undefined;
		}

		public currentQuestion: Models.Question;
		public stepNum: number;

		public dataGridOptions: ngGrid.IGridOptions = {
			data: "vm.DataMonitors",
			columnDefs: [
				{ cellTemplate: "../../partials/monitor-cell-template.html", width: 40, maxWidth: 40 },
				{ field: "description", displayName: "" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public autoGridOptions: ngGrid.IGridOptions = {
			data: "vm.AutoMonitors",
			columnDefs: [
				{ cellTemplate: "../../partials/monitor-cell-template.html", width: 40, maxWidth: 40 },
				{ field: "description", displayName: "" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public definesGridOptions: ngGrid.IGridOptions = {
			data: "vm.DefinesSimple",
			columnDefs: [
				{ field: "name", displayName: "Name" },
				{ field: "key", displayName: "Key" },
				{ field: "value", displayName: "Value" },
				{ cellTemplate: "../../partials/defines-cell-template.html" }
			],
			enableCellSelection: false,
			enableRowSelection: false,
			multiSelect: false,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public definesGridOptions1: ngGrid.IGridOptions = {
			data: "vm.DefinesSimple",
			columnDefs: [
				{ field: "name", displayName: "Name" },
				{ field: "key", displayName: "Key" },
				{ field: "value", displayName: "Value" }
			],
			enableCellSelection: false,
			enableRowSelection: false,
			multiSelect: false,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public get FaultMonitors(): Models.Agent {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.FaultMonitors[0];
			else
				return undefined;
		}

		public get DataMonitors(): Models.Agent[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.DataMonitors;
			else
				return undefined;
		}

		public get AutoMonitors(): Models.Agent[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.AutoMonitors;
			else
				return undefined;
		}

		public get DefinesSimple(): any[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.Defines.config;
			else
				return [];
		}

		public get title(): string {
			switch (this.params.step) {
				case StepNames.SetVars:
					return "Set Variables";
				case StepNames.Fault:
					return "Fault Detection";
				case StepNames.Data:
					return "Data Collection";
				case StepNames.Auto:
					return "Automation";
				default:
					return this.params.step;
			}
		}

		public get TestComplete(): boolean {
			return this.pitConfigSvc.TestComplete;
		}

		//#endregion


		//#region Public Methods

		public next() {
			if (this.currentQuestion == undefined) {
				//if we're not on a question, get the 0th
				this.questionPath = [];
				this.currentQuestion = <Models.Question>$.grep(this.qa, function (e) {return e.id == 0 })[0];
			}
			else {
				this.setThisStepIncomplete();

				var q = this.currentQuestion;
				if (q.type != Models.QuestionTypes.Jump) {
					// push this question id onto the path stack
					this.questionPath.push(q.id);
				}

				//store the value in the state bag
				if (q.key != undefined) {
					if (q.value == undefined && q.required == false) {
					}
					else {
						this.pitConfigSvc.StateBag.s(q.key, q.value);
					}
				}

				var nextid: number;

				if ([Models.QuestionTypes.Choice, Models.QuestionTypes.Jump].indexOf(q.type) >= 0) {
					// get next id from selected choice
					var choice = $.grep(q.choice, function (e) {
						if (e.value == undefined && e.next != undefined)
							return e.next.toString() == q.value.toString();
						else if (e.value != undefined && q.value != undefined)
							return e.value.toString() == q.value.toString();
						else
							return false;
					})[0];

					if (choice == undefined) {
						if (q.value != undefined) {
							nextid = q.choice[parseInt(q.value)].next;
						}
					}
					else {
						nextid = choice.next;
					}
				}
				else {
					// get the next question
					nextid = q.next;
				}

				if (nextid == undefined) {
					this.currentQuestion = new Models.Question();
					this.currentQuestion.type = Models.QuestionTypes.Done;
					switch (this.params.step) {
						case StepNames.SetVars:
							this.pitConfigSvc.Defines.LoadValuesFromStateBag(this.pitConfigSvc.StateBag);
							this.currentQuestion.qref = "/partials/setvars-done.html";
							break;
						case StepNames.Fault:
							this.pitConfigSvc.FaultMonitors = this.findMonitors();
							this.currentQuestion.qref = "/partials/fault-done.html";
							break;
						case StepNames.Data:
							this.pitConfigSvc.DataMonitors = this.pitConfigSvc.DataMonitors.concat(this.findMonitors());
							this.currentQuestion.qref = "/partials/data-done.html";
							break;
						case StepNames.Auto:
							this.pitConfigSvc.AutoMonitors = this.pitConfigSvc.AutoMonitors.concat(this.findMonitors());
							this.currentQuestion.qref = "/partials/auto-done.html";
							break;
					}
				}
				else {
					this.currentQuestion = <Models.Question>$.grep(this.qa, function (e) { return e.id == nextid; })[0];
				}
			}

			//get value from the state bag if necessary
			if (this.currentQuestion.value == undefined) {
				this.currentQuestion.value = this.pitConfigSvc.StateBag.g(this.currentQuestion.key);
			}

			this.stepNum = 2;

			// special stuff to do based on the next question
			switch (this.currentQuestion.type) {
				case Models.QuestionTypes.Intro:
					this.stepNum = 1;
					break;
				case Models.QuestionTypes.Jump:
					this.next();
					break;
				case Models.QuestionTypes.Choice:

					// what to do when there's no value already set for a choice question
					// if the first choice has a value, set default selection to first
					// if the first choice doesn't have a value it's a un-keyed choice, set default to 0
					// look at q-choice.html to see how its binding works
					if (this.currentQuestion.value == undefined) {
						if (this.currentQuestion.choice[0].value == undefined)
							this.currentQuestion.value = this.currentQuestion.choice[0].next;
						else
							this.currentQuestion.value = this.currentQuestion.choice[0].value;
					}
					break;
				case Models.QuestionTypes.Range:
					if (this.currentQuestion.value == undefined)
						this.currentQuestion.value = this.currentQuestion.rangeMin;
					break;
				case Models.QuestionTypes.Done:
					this.stepNum = 3;
					break;
				default:

					break;
			}

		}

		public back() {
			// pop the path and get the question

			var previousid = 0;
			if (this.questionPath.length > 0)
				previousid = this.questionPath.pop();

			this.currentQuestion = $.grep(this.qa, function (e) { return e.id == previousid; })[0];

			if (this.currentQuestion.type == Models.QuestionTypes.Intro)
				this.stepNum = 1;
			else if (this.currentQuestion.type == Models.QuestionTypes.Done)
				this.stepNum = 3;
			else
				this.stepNum = 2;

		}

		//#region Step Functions
		public completeIntro() {
			this.pitConfigSvc.IntroComplete = true;
			this.location.path("/quickstart/setvars");
		}

		public submitSetVarsInfo() {
			this.pitConfigSvc.SetVarsComplete = true;
			this.pitConfigSvc.Defines.LoadValuesFromStateBag(this.pitConfigSvc.StateBag);
			this.location.path("/quickstart/fault");
		}

		public restartFaultDetection() {
			this.pitConfigSvc.FaultMonitors = [];
			this.currentQuestion = undefined;
			this.pitConfigSvc.InitializeStateBag();
			this.next();
		}

		public submitFaultInfo() {
			this.pitConfigSvc.FaultMonitorsComplete = true;
			this.location.path("/quickstart/data");
		}

		public addNewDataInfo() {
			this.pitConfigSvc.InitializeStateBag();
			this.currentQuestion = undefined;
			this.next();
		}

		public submitDataInfo() {
			this.pitConfigSvc.DataMonitorsComplete = true;
			this.location.path("/quickstart/auto");
		}

		public addNewAutoInfo() {
			this.pitConfigSvc.InitializeStateBag();
			this.currentQuestion = undefined;
			this.next();
		}

		public submitAutoInfo() {
			this.pitConfigSvc.AutoMonitorsComplete = true;
			this.location.path("/quickstart/test");
		}

		public removeMonitor(index: number) {
			switch (this.params.step) {
				case "data":
					this.pitConfigSvc.DataMonitors.splice(index, 1);
					break;
				case "auto":
					this.pitConfigSvc.AutoMonitors.splice(index, 1);
					break;
			}
		}

		public Done() {
			this.pitConfigSvc.DoneComplete = true;
			this.location.path("/");
		}

		//#endregion


		public get CanStart() {
			return (this.pitConfigSvc.CanStartJob);
		}

		public Start() {
			this.pitConfigSvc.StartJob();
			this.Done();
		}

		public insertDefine(row) {
			if (this.currentQuestion.value == undefined) {
				this.currentQuestion.value = "";
			}
			this.currentQuestion.value += "##" + row.getProperty("key") + "##";
		}

		//#endregion


		//#region private functions
		private setThisStepIncomplete() {
			switch (this.params.step) {
				case StepNames.SetVars:
					this.pitConfigSvc.SetVarsComplete = false;
					break;
				case StepNames.Fault:
					this.pitConfigSvc.FaultMonitorsComplete = false;
					break;
				case StepNames.Data:
					this.pitConfigSvc.DataMonitorsComplete = false;
					break;
				case StepNames.Auto:
					this.pitConfigSvc.AutoMonitorsComplete = false;
					break;
				default:
					return;
			}
		}

		private refreshData(scope: ViewModelScope) {
			var res: ng.resource.IResourceClass<ng.resource.IResource<any>>;
			this.pitConfigSvc.InitializeStateBag();
			switch (this.params.step) {
				case StepNames.SetVars:
					this.pitConfigSvc.InitializeSetVars();
					this.next();
					return;
					break;
				case StepNames.Fault:
					//this.pitConfigSvc.FaultMonitors = [];
					res = this.peach.GetFaultQA();
					break;
				case StepNames.Data:
					//this.pitConfigSvc.DataMonitors = [];
					res = this.peach.GetDataQA();
					break;
				case StepNames.Auto:
					//this.pitConfigSvc.AutoMonitors = []; 
					res = this.peach.GetAutoQA();
					break;
				default:
					return;
			}

			res.get((data) => {
				this.pitConfigSvc.LoadData(data);
				this.next();
			}, this.getDataError);
		}

		private getDataError(something) {
			console.log(something);
		}

		public findMonitors(): Models.Agent[] {
			var foundMonitors: Models.Monitor[] = [];
			var agents: Models.Agent[] = [];

			foundMonitors = $.grep(this.monitors, (m) => {
				var w = $.grep(m.path, (p) => {
					return this.questionPath.indexOf(p) >= 0;
				});

				return (w.length == m.path.length);
			});

			if (foundMonitors.length > 0) {
				for (var m = 0; m < foundMonitors.length; m++) {
					var map = foundMonitors[m].map;
					foundMonitors[m].map = [];
					for (var i = 0; i < map.length; i++) {

						if (this.pitConfigSvc.StateBag.containsKey(map[i].key)) {
							var stateval = this.pitConfigSvc.StateBag.g(map[i].key);
							map[i].value = stateval;
							foundMonitors[m].map.push(map[i]);
						}
					}
				}
				var islocal: boolean = this.pitConfigSvc.StateBag.g("IsLocal");
				var agent: Models.Agent = new Models.Agent();
				if (islocal)
					agent.agentUrl = "local://";
				else
					agent.agentUrl = "tcp://" + this.pitConfigSvc.StateBag.g("AgentUrl");

				agent.monitors = foundMonitors;

				for (var i = 0; i < agent.monitors.length; i++) {
					if (agent.monitors[i].description != undefined) {
						agent.description += agent.monitors[i].description.replace(/\{\{|\}\}|\{(\w+)\}/g, (a, b) => {
							if (b == "AgentUrl")
								return agent.agentUrl;
							else
								return this.pitConfigSvc.StateBag.g(b);
						}) + "\n";
					}
				}

				agents.push(agent);
			}

			return agents;
		}
		//#endregion
	}

	export class StepNames {
		static Intro: string = "intro";
		static SetVars: string = "setvars";
		static Fault: string = "fault";
		static Data: string = "data";
		static Auto: string = "auto";
		static Test: string = "test";
		static Done: string = "done";
	}
}
