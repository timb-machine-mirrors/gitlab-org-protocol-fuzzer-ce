/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../scripts/typings/angularjs/angular-local-storage.d.ts" />
/// <reference path="../../../scripts/typings/angularjs/angular-route.d.ts" />

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
		private qa: W.Question[];
		private monitors: W.MonitorDefinition[];
		private location: ng.ILocationService;
		private peach: Services.IPeachService;
		private localStorage: ng.ILocalStorageService;
		public state: W.StateBag;
		private faultSummary: string;
		//#endregion

		//#region Public Properties
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

		get dataMonitors(): W.MonitorDefinition[] {
			this._storedMonitors = <W.MonitorDefinition[]>this.localStorage.get("dataMonitors");
			if (this._storedMonitors == undefined)
				this._storedMonitors = this.findMonitors();
			else
				this._storedMonitors = this._storedMonitors.concat(this.findMonitors());
			return this._storedMonitors;
		}

		get dataGridOptions(): any {
			return {
				data: "vm.dataMonitors",
				columnDefs: [
					{ field: "monitorClass", displayName: "Monitor" }
				]
			};
		}


		private _storedMonitors: W.MonitorDefinition[];

		get autoMonitors(): W.MonitorDefinition[]{
			if (this._storedMonitors == undefined) {
				this._storedMonitors = <W.MonitorDefinition[]>this.localStorage.get("autoMonitors");
				if (this._storedMonitors == undefined)
					this._storedMonitors = this.findMonitors();
				else
					this._storedMonitors = this._storedMonitors.concat(this.findMonitors());
			}
			return this._storedMonitors;
		}

		get autoGridOptions(): any {
			return {
				data: "vm.autoMonitors",
				columnDefs: [
					{ field: "monitorClass", displayName: "Monitor" }
				]
			};
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
		static $inject = ["$scope", "$routeParams", "$location", "peachService", "localStorageService"];

		constructor($scope: ViewModelScope, $routeParams: IWizardParams, $location: ng.ILocationService, peachService: Services.IPeachService, localStorageService: ng.ILocalStorageService) {
			this.params = $routeParams;

			$scope.vm = this;
			this.location = $location;
			this.peach = peachService;
			this.localStorage = localStorageService;

			if(this.params.step != undefined)
				this.refreshData($scope);
		}
		//#endregion

		//#region Public Methods

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
					this.state.s(q.key, q.value);
				}

				var nextid: number;

				if ([ W.QuestionTypes.Choice, W.QuestionTypes.Jump ].indexOf(q.type) >= 0) {
					// get next id from selected choice
					var choice = $.grep(q.choice, function (e)
					{
						if (e.value == undefined)
							return e.next.toString() == q.value.toString();
						else
							return e.value.toString() == q.value.toString();
					})[0];

					if (choice == undefined)
						nextid = q.choice[parseInt(q.value)].next;
					else
						nextid = choice.next;
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
							this.currentQuestion.qref = "/partials/setvars-done.html";
							break;
						case "fault":
							this.currentQuestion.qref = "/partials/fault-done.html";
							break;
						case "data":
							this.currentQuestion.qref = "/partials/data-done.html";
							break;
						case "auto":
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
				this.currentQuestion.value = this.state.g(this.currentQuestion.key);
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
			this.faultSummary = "";
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
			this.faultSummary = "";
		}

		public getFaultSummary(): string{
			if (this.faultSummary.length == 0) {
				var foundMonitors = this.findMonitors();
				this.faultSummary = JSON.stringify(foundMonitors);
			}
			return this.faultSummary;
		}

		public submitSetVarsInfo() {
			//TODO
			this.localStorage.set(StorageStrings.PitDefines, this.state);
			WizardController._setvarsclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/fault");
		}

		public submitFaultInfo() {
			var foundMonitors = this.findMonitors();
			this.storeMonitors(StorageStrings.FaultMonitors, foundMonitors, false);
			WizardController._faultclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/data");
		}

		public addNewDataInfo() {
			//TODO
			var foundMonitors = this.findMonitors();
			this.storeMonitors(StorageStrings.DataMonitors, foundMonitors, true);
			this.currentQuestion = undefined;
			this.next();
		}

		public submitDataInfo() {
			var foundMonitors = this.findMonitors();
			this.storeMonitors(StorageStrings.DataMonitors, foundMonitors, true);
			WizardController._dataclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/auto");
		}

		public submitAutoInfo() {
			var foundMonitors = this.findMonitors();
			this.localStorage.add(StorageStrings.AutoMonitors, foundMonitors);
			WizardController._autoclass = W.ConfiguratorStepClasses.Complete;
			this.location.path("/configurator/test");
		}

		//#endregion

		//#region private functions
		private getDefines(): W.StateBag {
			return <W.StateBag>this.localStorage.get(StorageStrings.PitDefines);
		}

		private getDefineValue(key: string): any {
			var defines = this.getDefines();
			return defines.g(key);
		}

		//private getDefinesKeys(): string[] {
		//	var defines = this.getDefines();
		//	return $.grep<Models.StateItem(defines).
		//}

		private refreshData(scope: ViewModelScope) {
			var that = this;
			var res: ng.resource.IResourceClass<ng.resource.IResource<any>>;

			switch (this.params.step) {
				case "setvars":
					this.localStorage.remove(StorageStrings.PitDefines);
					res = this.peach.GetDefines((<P.Pit>this.localStorage.get("pit")).pitUrl);
					break;
				case "fault":
					this.localStorage.remove(StorageStrings.FaultMonitors);
					res = this.peach.GetFaultQA();
					break;
				case "data":
					this.localStorage.remove(StorageStrings.DataMonitors);
					res = this.peach.GetDataQA();
					break;
				case "auto":
					this.localStorage.remove(StorageStrings.AutoMonitors);
					res = this.peach.GetAutoQA();
					break;
				default:
					return;
			}

			res.get(function (data) {
				if (data.config != undefined)
					that.qa = that.convertConfig(<Bork[]>data.config);

				if(data.qa != undefined)
					that.qa = <W.Question[]>data.qa;

				if (data.state != undefined)
					that.state = new W.StateBag(<any[]>data.state);
				else
					that.state = new W.StateBag();

				if(data.monitors != undefined)
					that.monitors = <W.MonitorDefinition[]>data.monitors;

				that.next();
			}, this.getDataError);
		}


		
		private convertConfig(config: Bork[]): W.Question[] {
			var output: W.Question[] = [];
			for (var i = 0; i < config.length; i++) {
				var question = new W.Question;
				question.id = i;
				if (config[i].description != undefined)
					question.q = config[i].description;
				else
					question.q = config[i].key;

				question.key = config[i].key;
				question.type = config[i].type;
				question.value = config[i].value;
				question.defaults = config[i].defaults;
				if ((i + 1) < config.length) {
					question.next = i + 1;
				}
				output.push(question);
			}
			return output;
		}

		private getDataError(something) {
			console.log(something);
		}

		public findMonitors(): W.MonitorDefinition[] {
			var that = this;
			var foundMonitors: W.MonitorDefinition[] = [];
			foundMonitors = $.grep(this.monitors, function (m) {
				var w = $.grep(m.path, function (p) {
					return that.questionPath.indexOf(p) >= 0;
				});

				return (w.length == m.path.length);
			});

			if (foundMonitors.length > 0) {
				for (var m = 0; m < foundMonitors.length; m++) {
					for (var i = 0; i < foundMonitors[m].map.length; i++) {
						if (this.state.containsKey(foundMonitors[m].map[i].key)) {
							var stateval = this.state.g(foundMonitors[m].map[i].key);
							foundMonitors[m].map[i].value = stateval;
						}
					}
				}
			}

			return foundMonitors;
		}

		private storeMonitors(key: string, monitors: W.MonitorDefinition[], concat: boolean) {
			if (concat) {
				var dataMonitors = <W.MonitorDefinition[]>this.localStorage.get(key);
				if (dataMonitors == undefined)
					dataMonitors = monitors;
				else
					dataMonitors = dataMonitors.concat(monitors);

				this.localStorage.set(key, dataMonitors);
			}
			else {
				this.localStorage.remove(key);
				this.localStorage.add(key, monitors);
			}
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

	interface Bork {
		key: string;
		value: any;
		description: string;
		type: string;
		defaults: any[];
	}
}
