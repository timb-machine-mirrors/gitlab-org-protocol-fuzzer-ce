/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IFormScope extends IViewModelScope {
		form: ng.IFormController;
	}

	export class ConfigureMonitorsController {
		public AvailableMonitors: ng.resource.IResource<Models.IMonitor>[];
		public Model: Models.IPitAgents;

		static $inject = [
			"$scope",
			"$timeout",
			"PitService",
			"AvailableMonitorsResource"
		];

		constructor(
			private $scope: IFormScope,
			private $timeout: ng.ITimeoutService,
			private pitService: Services.PitService,
			availableMonitorsResource : Models.IMonitorResource
		) {
			$scope.vm = this;
			this.AvailableMonitors = availableMonitorsResource.query();
			pitService.LoadPitConfig();
			this.Model = pitService.LoadPitAgents();
		}

		public get ShowSaved(): boolean {
			return !this.$scope.form.$dirty && !this.$scope.form.$pristine;
		}

		public get ShowError(): boolean {
			return this.$scope.form.$invalid && this.$scope.form.$dirty;
		}

		public get ShowIntro(): boolean {
			return this.numAgents === 0;
		}

		public get CanSave(): boolean {
			return this.$scope.form.$dirty && !this.$scope.form.$invalid && this.numMonitors > 0;
		}

		public AddAgent(): void {
			this.Model.agents.push(new Models.Agent());
		}

		public RemoveAgent(agentIndex: number): void {
			this.Model.agents.splice(agentIndex, 1);
			this.$scope.form.$setDirty();
		}

		public AgentUp(agentIndex: number): void {
			this.$timeout(() => {
				this.Model.agents = this.arrayItemUp(this.Model.agents, agentIndex);
			});
		}

		public AgentDown(agentIndex: number): void {
			this.$timeout(() => {
				this.Model.agents = this.arrayItemDown(this.Model.agents, agentIndex);
			});
		}

		public AddMonitor(agent: Models.Agent, monitor: Models.IMonitor): void {

			for (var i = 0; i < monitor.map.length; i++) {
				switch (monitor.map[i].type) {
					case "bool":
						monitor.map[i].value = (monitor.map[i].value === 'true');
						break;
					case "int":
						monitor.map[i].value = parseInt(monitor.map[i].value);
						break;
				}
			}
			agent.monitors.push(monitor);
		}

		public RemoveMonitor(agentIndex: number, monitorIndex: number): void {
			this.Model.agents[agentIndex].monitors.splice(monitorIndex, 1);
			this.$scope.form.$setDirty();
		}

		public MonitorUp(agentIndex: number, monitorIndex: number): void {
			this.$timeout(() => {
				this.Model.agents[agentIndex].monitors = this.arrayItemUp(
					this.Model.agents[agentIndex].monitors,
					monitorIndex
				);
			});
		}

		public MonitorDown(agentIndex: number, monitorIndex: number): void {
			this.$timeout(() => {
				this.Model.agents[agentIndex].monitors = this.arrayItemDown(
					this.Model.agents[agentIndex].monitors,
					monitorIndex
				);
			});
		}

		public Save(): void {
			this.Model.$save({ id: this.pitService.PitId }, () => {
				this.$scope.form.$dirty = false;
			});
		}

		public get BooleanChoices(): string[] {
			return this.defines().concat(['true', 'false']);
		}

		public EnumChoices(defaults: string[]): string[] {
			return this.defines().concat(defaults);
		}

		private defines(): string[] {
			var names = _.pluck(this.pitService.PitConfig.config, 'key');
			return _.map(names, x => '##' + x + '##');
		}

		private get numAgents(): number {
			if (this.Model && this.Model.$resolved) {
				return this.Model.agents.length;
			}
			return 0;
		}

		private get numMonitors(): number {
			if (this.Model && this.Model.$resolved && this.Model.agents.length) {
				return _.first(this.Model.agents).monitors.length;
			}
			return 0;
		}

		private arrayItemUp<T>(array: T[], i: number): T[]{
			if (i > 0) {
				var x = array[i - 1];
				array[i - 1] = array[i];
				array[i] = x;
			}
			return array;
		}

		private arrayItemDown<T>(array: T[], i: number): T[] {
			if (i < array.length - 1) {
				var x = array[i + 1];
				array[i + 1] = array[i];
				array[i] = x;
			}
			return array;
		}
	}
}
