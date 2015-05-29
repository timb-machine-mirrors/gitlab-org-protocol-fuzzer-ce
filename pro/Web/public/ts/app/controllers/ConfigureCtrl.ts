/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ConfigureController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Pit,
			C.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private pitService: PitService,
			private jobService: JobService
		) {
			this.pitService.LoadPit().then((pit: IPit) => {
				this.Job = {
					pitUrl: pit.pitUrl
				};
			})
		}

		public IsOpen: boolean = false;
		public Job: IJobRequest;

		public get ShowReady(): boolean {
			return onlyIf(this.pitService.Pit, () => 
				this.pitService.IsConfigured && this.CanStart);
		}

		public get ShowNotConfigured(): boolean {
			return onlyIf(this.pitService.Pit, () => !this.pitService.IsConfigured);
		}

		public get CanStart(): boolean {
			return this.jobService.CanStart;
		}

		public Start() {
			this.jobService.Start(this.Job)
				.then((job: IJob) => {
					this.$state.go(C.States.Job, { job: job.id });
				});
		}
	}
}
