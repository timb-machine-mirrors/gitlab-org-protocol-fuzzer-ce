/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ConfigureMonitorsController {
		public Model: IPitAgents;

		static $inject = [
			"$scope",
			"$timeout",
			"PitService"
		];

		constructor(
			private $scope: IFormScope,
			private $timeout: ng.ITimeoutService,
			private pitService: PitService,
			availableMonitorsResource : IMonitorResource
		) {
			$scope.vm = this;
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
			this.Model.agents.push(new Agent());
			this.$scope.form.$setDirty();
		}

		public Save(): void {
			this.Model.$save({ id: this.pitService.PitId }, () => {
				this.$scope.form.$dirty = false;
			});
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
	}
}
