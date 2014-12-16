/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class DashboardController {

		static $inject = [
			"$scope",
			"$modal",
			"PitService",
			"JobService"
		];

		constructor(
			$scope: IViewModelScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService,
			private jobService: JobService
		) {
			$scope.vm = this;

			$scope.$watch('vm.jobService.Faults.length', (newVal, oldVal) => {
				if (newVal !== oldVal) {
					this.refreshFaults();
				}
			});

			this.refreshFaults();
		}

		public Faults: IFaultSummary[] = [];

		public get ShowSelectPit(): boolean {
			return !this.Job && !this.pitService.Pit;
		}

		public get ShowNeedsConfig(): boolean {
			return onlyIf(this.pitService.Pit, () => !this.pitService.IsConfigured);
		}

		public get ShowLimited(): boolean {
			return onlyIf(this.Job, () => _.isEmpty(this.Job.pitUrl));
		}

		public get ShowReproducing(): boolean {
			return onlyIf(this.Job, () => this.Job.mode === JobMode.Reproducing);
		}

		public get ShowSearching(): boolean {
			return onlyIf(this.Job, () => this.Job.mode === JobMode.Searching);
		}

		public get Job(): IJob {
			return this.jobService.Job;
		}

		public get RunningTime(): string {
			return this.jobService.RunningTime;
		}

		public get CanStart() {
			return this.jobService.CanStartJob || this.jobService.CanContinueJob;
		}

		public get CanPause() {
			return this.jobService.CanPauseJob;
		}

		public get CanStop() {
			return this.jobService.CanStopJob;
		}

		public StartWithOptions() {
			this.$modal.open({
				templateUrl: "html/modal/StartJob.html",
				controller: StartJobController
			}).result.then((job: IJob) => {
				this.jobService.StartJob(job);
			});
		}

		public Start() {
			this.jobService.StartJob();
		}

		public Pause() {
			this.jobService.PauseJob();
		}

		public Stop() {
			this.jobService.StopJob();
		}

		private refreshFaults() {
			this.Faults = _.last(this.jobService.Faults, 10).reverse();
		}
	}
}
