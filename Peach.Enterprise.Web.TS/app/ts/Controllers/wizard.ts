/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular-local-storage.d.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular-route.d.ts" />

module DashApp {
	"use strict";

	import W = DashApp.Models.Wizard;
	import P = DashApp.Models.Peach;
	
	export interface IWizardParams extends ng.route.IRouteParamsService {
		step: string;
	}

	export class WizardController {
		//#region private variables
		private params: IWizardParams;
		private questionPath:number[] = [0];
		
		private location: ng.ILocationService;
		private peach: Services.IPeachService;
		private pitConfigSvc: Services.IPitConfiguratorService;
		//#endregion

		//#region Public Properties
		private _isDefaultsOpen: boolean = false;

		public get isDefaultsOpen(): boolean {
			return this._isDefaultsOpen;
		}

		public set isDefaultsOpen(value: boolean) {
			if (value == false || this.currentQuestion.defaults.length > 0) {
				this._isDefaultsOpen = value;
			}
		}

		public get qa(): W.Question[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.QA;
			else
				return undefined;
		}

		public get monitors(): W.Monitor[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.Monitors;
			else
				return undefined;
		}

		public get defines(): P.PitConfig {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.Defines;
			else
				return undefined;
		}

		public currentQuestion: W.Question;
		public stepNum: number;

		private static _introclass: string = "";
		get introclass(): string {
			return WizardController._introclass;
		}

		private static _setvarsclass: string = "";
		get setvarsclass(): string {
			return WizardController._setvarsclass;
		}

		private static _faultclass: string = "";
		get faultclass(): string {
			return WizardController._faultclass;
		}

		private static _dataclass: string = "";
		get dataclass(): string {
			return WizardController._dataclass;
		}

		private static _autoclass: string = "";
		get autoclass(): string {
			return WizardController._autoclass;
		}

		private static _testclass: string = "";
		get testclass(): string {
			return WizardController._testclass;
		}

		private static _doneclass: string = "";
		get doneclass(): string {
			return WizardController._doneclass;
		}

		public get dataGridOptions(): any {
			return {
				data: "vm.DataMonitors",
				columnDefs: [
					{ field: "description", displayName: "" },
					{ cellTemplate: "../../partials/monitor-cell-template.html" }
				]
			};
		}

		public get autoGridOptions(): any {
			return {
				data: "vm.AutoMonitors",
				columnDefs: [
					{ field: "description", displayName: "" },
					{ cellTemplate: "../../partials/monitor-cell-template.html" }
				]
			};
		}

		public get definesGridOptions(): any {
			return {
				data: "vm.DefinesSimple",
				columnDefs: [
					{ field: "name", displayName: "Name" },
					{ field: "key", displayName: "Key" },
					{ field: "value", displayName: "Value" },
					{ cellTemplate: "../../partials/defines-cell-template.html"}
				],
				enableCellSelection: false,
				enableRowSelection: false,
				multiSelect: false
			};
		}

		public get definesGridOptions1(): any {
			return {
				data: "vm.DefinesSimple",
				columnDefs: [
					{ field: "name", displayName: "Name" },
					{ field: "key", displayName: "Key" },
					{ field: "value", displayName: "Value" }
				],
				enableCellSelection: false,
				enableRowSelection: false,
				multiSelect: false
			};
		}

		public get FaultMonitors(): W.Agent{
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.FaultMonitors[0];
			else
				return undefined;
		}

		public get DataMonitors(): W.Agent[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.DataMonitors;
			else
				return undefined;
		}

		public get AutoMonitors(): W.Agent[] {
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
				case "setvars":
					return "Set Variables";
				case "fault":
					return "Fault Detection";
				case "data":
					return "Data Collection";
				case "auto":
					return "Automation";
				default:
					return "";
			}
		}

		//#endregion

		//#region ctor
		static $inject = ["$scope", "$routeParams", "$location", "peachService", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, $routeParams: IWizardParams, $location: ng.ILocationService, peachService: Services.IPeachService, pitConfiguratorService: Services.IPitConfiguratorService) {
			this.params = $routeParams;

			$scope.vm = this;
			this.location = $location;
			this.peach = peachService;
			this.pitConfigSvc = pitConfiguratorService;

			if(this.params.step != undefined)
				this.refreshData($scope);
		}
		//#endregion

		//#region Public Methods
		public insertDefine(row) {
			if (this.currentQuestion.value == undefined) {
				this.currentQuestion.value = "";
			}
			this.currentQuestion.value += "##" + row.getProperty("key") + "##";
		}

		public getTemplateUrl(): string {
			if (this.currentQuestion != undefined) {
				if (this.currentQuestion.qref != undefined)
					return this.currentQuestion.qref;
				else
					return "/partials/q-" + this.currentQuestion.type + ".html";
			}
			else
				return "";
		}

		public next() {
			if (this.currentQuestion == undefined) {
				//if we're not on a question, get the 0th
				this.questionPath = [];
				this.currentQuestion = <W.Question>$.grep(this.qa, function (e) {return e.id == 0 })[0];
			}
			else {
				var q = this.currentQuestion;
				if (q.id != 0 && q.type != W.QuestionTypes.Jump) {
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

				if ([ W.QuestionTypes.Choice, W.QuestionTypes.Jump ].indexOf(q.type) >= 0) {
					// get next id from selected choice
					var choice = $.grep(q.choice, function (e)
					{
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
					this.currentQuestion = new W.Question();
					this.currentQuestion.type = W.QuestionTypes.Done;
					switch (this.params.step) {
						case "setvars":
							this.pitConfigSvc.Defines.LoadValuesFromStateBag(this.pitConfigSvc.StateBag);
							this.currentQuestion.qref = "/partials/setvars-done.html";
							break;
						case "fault":
							this.pitConfigSvc.FaultMonitors = this.findMonitors();
							this.currentQuestion.qref = "/partials/fault-done.html";
							break;
						case "data":
							this.pitConfigSvc.DataMonitors = this.pitConfigSvc.DataMonitors.concat(this.findMonitors());
							this.currentQuestion.qref = "/partials/data-done.html";
							break;
						case "auto":
							this.pitConfigSvc.AutoMonitors = this.pitConfigSvc.DataMonitors.concat(this.findMonitors());
							this.currentQuestion.qref = "/partials/auto-done.html";
							break;
					}
				}
				else {
					this.currentQuestion = <W.Question>$.grep(this.qa, function (e) { return e.id == nextid; })[0];
				}

			}
			
			//get value from the state bag if necessary
			if (this.currentQuestion.value == undefined) {
				//this.currentQuestion.value = this.state.g(this.currentQuestion.key);
				this.currentQuestion.value = this.pitConfigSvc.StateBag.g(this.currentQuestion.key);
			}

			this.stepNum = 2;

			// special stuff to do based on the next question
			switch (this.currentQuestion.type) {
				case W.QuestionTypes.Intro:
					this.stepNum = 1;
					break;
				case W.QuestionTypes.Jump:
					this.next();
					break;
				case W.QuestionTypes.Choice:

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
				case W.QuestionTypes.Range:
					if (this.currentQuestion.value == undefined)
						this.currentQuestion.value = this.currentQuestion.rangeMin;
					break;
				case W.QuestionTypes.Done:
					this.stepNum = 3;
					break;
				default:

					break;
			}

			this.resetStepClass();
		}

		public back()	{
			// pop the path and get the question

			var previousid = 0;
			if(this.questionPath.length > 1)
				previousid = this.questionPath.pop();

			this.currentQuestion = $.grep(this.qa, function (e) { return e.id == previousid; })[0];
			if (this.currentQuestion.type == W.QuestionTypes.Intro)
				this.stepNum = 1;
			else if (this.currentQuestion.type == W.QuestionTypes.Done)
				this.stepNum = 3;
			else
				this.stepNum = 2;

			this.resetStepClass();
		}

		public restartFaultDetection() {
			this.pitConfigSvc.FaultMonitors = [];
			this.currentQuestion = undefined;
			this.resetStepClass();
			this.next();
		}

		public submitSetVarsInfo() {
			this.pitConfigSvc.Defines.LoadValuesFromStateBag(this.pitConfigSvc.StateBag);
			WizardController._setvarsclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/fault");
		}

		public submitFaultInfo() {
			WizardController._faultclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/data");
		}

		public addNewDataInfo() {
			this.currentQuestion = undefined;
			this.next();
		}

		public submitDataInfo() {
			WizardController._dataclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/auto");
		}

		public submitAutoInfo() {
			WizardController._autoclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/test");
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

		//#endregion

		//#region private functions

		private refreshData(scope: ViewModelScope) {
			var res: ng.resource.IResourceClass<ng.resource.IResource<any>>;

			switch (this.params.step) {
				case "setvars":
					this.pitConfigSvc.QA = this.pitConfigSvc.Defines.ToQuestions();
					this.next();
					return;
					break;
				case "fault":
					this.pitConfigSvc.FaultMonitors = [];
					res = this.peach.GetFaultQA();
					break;
				case "data":
					this.pitConfigSvc.DataMonitors = [];
					res = this.peach.GetDataQA();
					break;
				case "auto":
					this.pitConfigSvc.AutoMonitors = [];
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

		public findMonitors(): W.Agent[] {
			var foundMonitors: W.Monitor[] = [];
			var agents: W.Agent[] = [];

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

				var agent: W.Agent = new W.Agent();
				agent.agentUrl = this.pitConfigSvc.StateBag.g("AgentUrl");
				if (agent.agentUrl == undefined)
					agent.agentUrl = "local://";
				else
					agent.agentUrl = "tcp://" + agent.agentUrl;

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

		private resetStepClass() {
			switch (this.params.step) {
				case "setvars":
					WizardController._setvarsclass = W.ConfiguratorStepClasses.None;
					break;
				case "fault":
					WizardController._faultclass = W.ConfiguratorStepClasses.None;
					break;
				case "data":
					WizardController._dataclass = W.ConfiguratorStepClasses.None;
					break;
				case "auto":
					WizardController._autoclass = W.ConfiguratorStepClasses.None;
					break;
			}
		}

		//#endregion
	}
}
