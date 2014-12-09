/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class AgentDirective implements ng.IDirective {
		public restrict = 'E';
		public templateUrl = 'html/directives/agent.html';
		public controller = 'AgentController';
		public scope = {
			agents: '=',
			agent: '=',
			agentIndex: '='
		};
	}

	export interface IAgentScope extends IFormScope {
		agents: Models.Agent[];
		agent: Models.Agent;
		agentIndex: number;
		isOpen: boolean;
	}

	export class AgentController {
		static $inject = [
			"$scope",
			"PitService",
			"AvailableMonitorsResource"
		];

		constructor(
			private $scope: IAgentScope,
			private pitService: Services.PitService,
			availableMonitorsResource: Models.IMonitorResource
		) {
			$scope.vm = this;
			$scope.isOpen = true;
			this.AvailableMonitors = availableMonitorsResource.query();
		}

		public AvailableMonitors: ng.resource.IResource<Models.IMonitor>[];

		public get CanMoveUp(): boolean {
			return this.$scope.agentIndex !== 0;
		}

		public get CanMoveDown(): boolean {
			return this.$scope.agentIndex !== (this.$scope.agents.length - 1);
		}

		public OnMoveUp($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemUp(this.$scope.agents, this.$scope.agentIndex);
			this.$scope.form.$setDirty();
		}

		public OnMoveDown($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemDown(this.$scope.agents, this.$scope.agentIndex);
			this.$scope.form.$setDirty();
		}

		public OnRemove($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$scope.agents.splice(this.$scope.agentIndex, 1);
			this.$scope.form.$setDirty();
		}

		public OnAddMonitor($event: ng.IAngularEvent, monitor: Models.IMonitor): void {
			$event.preventDefault();
			this.$scope.agent.monitors.push(monitor);
			this.$scope.form.$setDirty();
		}
	}
}
