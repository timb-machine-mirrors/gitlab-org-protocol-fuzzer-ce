/// <reference path="../reference.ts" />

namespace Peach {
	"use strict";

	export var AgentDirective: IDirective = {
		ComponentID: C.Directives.Agent,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Agent,
		controller: C.Controllers.Agent,
		scope: {
			agents: '=',
			agent: '=',
			agentIndex: '='
		}
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
			C.Angular.$scope,
			C.Angular.$timeout,
			C.Services.Pit
		];

		constructor(
			private $scope: IAgentScope,
			private $timeout: ng.ITimeoutService,
			private pitService: PitService
		) {
			$scope.vm = this;
			$scope.isOpen = true;
			$scope.selectedMonitor = {
				selected: undefined
			};
			pitService.LoadPeachMonitors().then((monitors: IMonitor[]) => {
				this.PeachMonitors = monitors;
			});
		}

		public PeachMonitors: IMonitor[];

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
