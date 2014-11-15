/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../Scripts/typings/ng-grid/ng-grid.d.ts" />
/// <reference path="../Models/models.ts" />
/// <reference path="../Services/peach.ts" />
/// <reference path="../Services/pitconfigurator.ts" />
/// <reference path="main.ts" />

module DashApp {
	"use strict";

	declare function ngGridFlexibleHeightPlugin(opts?: any): void; 

	export class ConfigurationMonitorsController {
		//#region private variables
		private peach: Services.IPeachService;
		private pitconfig: Services.IPitConfiguratorService;
		private timeout: ng.ITimeoutService;
		//#endregion

		//#region ctor
		static $inject = ["$scope", "$timeout", "peachService", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, $timeout: ng.ITimeoutService, peachService: Services.IPeachService, pitConfiguratorService: Services.IPitConfiguratorService) {

			$scope.vm = this;
			this.peach = peachService;
			this.pitconfig = pitConfiguratorService;
			this.timeout = $timeout;
			this.refreshData($scope);
		}
		//#endregion

		public IsDirty: boolean = true;

		public Agents: Models.Agent[] = [];

		public AvailableMonitors: Models.Monitor[] = [];

		public AddAgent(): void {
			this.Agents.push(new Models.Agent());
		}

		public RemoveAgent(agentIndex: number): void {
			this.Agents.splice(agentIndex, 1);
		}

		public AgentUp(agentIndex: number): void {
			this.timeout(() => {
				this.Agents = this.ArrayItemUp(this.Agents, agentIndex);
			});
		}

		public AgentDown(agentIndex: number): void {
			this.timeout(() => {
				this.Agents = this.ArrayItemDown(this.Agents, agentIndex);
			});
		}

		public AddMonitor(agent: Models.Agent, monitor: Models.Monitor): void {
			
			for (var i = 0; i < monitor.map.length; i++) {
				switch (monitor.map[i].type) {
					case "bool":
						monitor.map[i].value = (monitor.map[i].value === 'true');
						break;
					case "int":
						monitor.map[i].value = Number(monitor.map[i].value);
						break;
				}
			}
			agent.monitors.push(monitor);
		}

		public RemoveMonitor(agentIndex: number, monitorIndex: number): void {
			this.Agents[agentIndex].monitors.splice(monitorIndex, 1);
		}

		public MonitorUp(agentIndex: number, monitorIndex: number): void {
			this.timeout(() => {
				this.Agents[agentIndex].monitors = this.ArrayItemUp(this.Agents[agentIndex].monitors, monitorIndex);
			});
		}

		public MonitorDown(agentIndex: number, monitorIndex: number): void {
			this.timeout(() => {
				this.Agents[agentIndex].monitors = this.ArrayItemDown(this.Agents[agentIndex].monitors, monitorIndex);
			});
		}

		public Save(form: ng.IFormController): void {
			this.peach.PostMonitors(this.pitconfig.Pit.pitUrl, this.Agents).success(() => {
				form.$dirty = false;
			});
		}

		private refreshData(scope: ViewModelScope) {
			this.peach.GetMonitors((data: Models.Monitor[]) => {
				this.AvailableMonitors = data;
			});
		}

		private getDataError(something) {
			console.log(something);
		}

		private ArrayItemUp<T>(array: T[], i: number): T[]{
			if (i > 0) {
				var x = array[i - 1];
				array[i - 1] = array[i];
				array[i] = x;
				return array;
			}
			else
				return array;
		}

		private ArrayItemDown<T>(array: T[], i: number): T[] {
			if (i < array.length - 1) {
				var x = array[i + 1];
				array[i + 1] = array[i];
				array[i] = x;
				return array;
			}
			else
				return array;
		}
	}
}