/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ConfigureMonitorsController {
		public Agents: Agent[];

		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IFormScope,
			private pitService: PitService
		) {
			var promise = pitService.ReloadPit();
			promise.then((pit: IPit) => {
				this.Agents = pit.agents;
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
			this.Agents.push(new Agent());
			this.$scope.form.$setDirty();
		}

		public Save(): void {
			var promise = this.pitService.SaveAgents(this.Agents);
			promise.then(() => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		private get numAgents(): number {
			return (this.Agents && this.Agents.length) || 0;
		}
	}
}
