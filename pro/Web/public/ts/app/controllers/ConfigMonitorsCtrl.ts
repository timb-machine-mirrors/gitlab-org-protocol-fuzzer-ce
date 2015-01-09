/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ConfigureMonitorsController {
		public Model: IPitAgents;

		static $inject = [
			Constants.Angular.$scope,
			Constants.Services.Pit
		];

		constructor(
			private $scope: IFormScope,
			private pitService: PitService,
			availableMonitorsResource : IMonitorResource
		) {
			$scope.vm = this;
			var promise = pitService.LoadPitConfig();
			promise.then(() => {
				var promise2 = pitService.LoadPitAgents();
				promise2.then((agents: IPitAgents) => {
					var promise3 = pitService.LoadPitCalls();
					promise3.then(() => {
						this.Model = agents;
					});
				});
			});
		}

		private isSaved: boolean = false;

		public get ShowSaved(): boolean {
			return !this.$scope.form.$dirty && this.isSaved;
		}

		public get ShowError(): boolean {
			return this.$scope.form.$invalid && this.$scope.form.$dirty;
		}

		public get ShowMissingAgents(): boolean {
			return this.numAgents === 0;
		}

		public get CanSave(): boolean {
			return this.$scope.form.$dirty && !this.$scope.form.$invalid;
		}

		public AddAgent(): void {
			this.Model.agents.push(new Agent());
			this.$scope.form.$setDirty();
		}

		public Save(): void {
			this.Model.$save({ id: this.pitService.PitId }, () => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		private get numAgents(): number {
			if (this.Model && this.Model.$resolved) {
				return this.Model.agents.length;
			}
			return 0;
		}
	}
}
