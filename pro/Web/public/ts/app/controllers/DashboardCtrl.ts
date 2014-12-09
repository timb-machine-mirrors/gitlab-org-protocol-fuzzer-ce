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
			private pitService: Services.PitService,
			private jobService: Services.JobService
		) {
			$scope.vm = this;

			$scope.$watch('vm.jobService.Faults.length', (newVal, oldVal) => {
				if (newVal !== oldVal) {
					this.refreshFaults();
				}
			});

			this.refreshFaults();
		}

		public Faults: Models.IFaultSummary[] = [];

		public get IsConfigured(): boolean {
			return this.pitService.IsConfigured;
		}

		public get CanControlPit(): boolean {
			var pit = this.pitService.Pit;
			return pit && pit.pitUrl && pit.pitUrl.length > 0;
		}

		public get Job(): Models.IJob {
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
			}).result.then((job: Models.IJob) => {
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
