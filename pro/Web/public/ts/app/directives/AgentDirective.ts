/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class AgentDirective implements ng.IDirective {
		public restrict = 'E';
		public templateUrl = 'html/directives/agent.html';
		public controller = AgentController;
		public scope = {
			agents: '=',
			agent: '=',
			agentIndex: '='
		};
	}

	export interface ISelectable<T> {
		selected: T;
	}

	export interface IAgentScope extends IFormScope {
		agents: Agent[];
		agent: Agent;
		agentIndex: number;
		isOpen: boolean;
		selectedMonitor: ISelectable<IMonitor>;
	}

	export class AgentController {
		static $inject = [
			"$scope",
			"$timeout",
			"PitService",
			"AvailableMonitorsResource"
		];

		constructor(
			private $scope: IAgentScope,
			private $timeout: ng.ITimeoutService,
			private pitService: PitService,
			availableMonitorsResource: IMonitorResource
		) {
			$scope.vm = this;
			$scope.isOpen = true;
			$scope.selectedMonitor = {
				selected: undefined
			};
			this.AvailableMonitors = availableMonitorsResource.query();
		}

		public AvailableMonitors: ng.resource.IResource<IMonitor>[];

		public get Header(): string {
			var url = this.$scope.agent.agentUrl || 'local://';
			var name = this.$scope.agent.name ? '(' + this.$scope.agent.name + ')' : '';
			return url + ' ' + name;
		}

		public get CanMoveUp(): boolean {
			return this.$scope.agentIndex !== 0;
		}

		public get CanMoveDown(): boolean {
			return this.$scope.agentIndex !== (this.$scope.agents.length - 1);
		}

		public get ShowMissingMonitors(): boolean {
			return this.$scope.agent.monitors.length === 0;
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

		public OnAddMonitor(monitor: IMonitor): void {
			this.$scope.agent.monitors.push(angular.copy(monitor));
			this.$scope.form.$setDirty();

			this.$timeout(() => {
				this.$scope.selectedMonitor.selected = undefined;
			});
		}
	}
}
