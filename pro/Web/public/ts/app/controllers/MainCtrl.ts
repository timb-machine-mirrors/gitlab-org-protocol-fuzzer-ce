/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class MainController {

		static $inject = [
			"$scope",
			"$location",
			"$modal",
			"PitService",
			"JobService",
			"WizardService"
		];

		constructor(
			$scope: IViewModelScope,
			private $location: ng.ILocationService,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService,
			private jobService: JobService,
			private wizardService: WizardService
		) {
			$scope.vm = this;

			var promise = this.jobService.GetJobs();
			promise.then(() => {
				if (_.isUndefined(this.job)) {
					var pitId = $location.search()['pit'];
					if (pitId) {
						this.pitService.SelectPit('/p/pits/' + pitId);
					} else if (!this.pitService.Pit) {
						this.OnSelectPit();
					}
				}
			});
		}

		private get pit(): IPit {
			return this.pitService.Pit;
		}

		private get job(): IJob {
			return this.jobService.Job;
		}

		public IsActive(match): boolean {
			return this.$location.path() === match;
		}

		public get PitName(): string {
			return this.pitService.Name;
		}

		public get FaultCount(): any {
			var count = 0;
			if (this.job) {
				count = this.job.faultCount;
			}
			return count || '';
		}

		public get JobRunningTooltip(): string {
			if (this.isJobRunning) {
				return "Disabled while running a Job";
			}
			return "";
		}

		public get JobNotRunningTooltip(): string {
			if (!this.isJobRunning) {
				return "Disabled while not running a Job";
			}
			return "";
		}

		public get FaultsUnavailableTooltip(): string {
			if (_.isUndefined(this.job)) {
				return "No Job available";
			}
			return "";
		}

		public get MetricsUnavailableTooltip(): string {
			if (_.isUndefined(this.job)) {
				return "No Job available";
			}
			if (this.isJobRunning && this.job.hasMetrics == false) {
				return "Metrics unavailable for this Job.";
			}
			return "";
		}

		public IsComplete(step: string) {
			return this.wizardService.GetTrack(step).isComplete;
		}

		public get CanSelectPit(): boolean {
			return (
				_.isUndefined(this.job) ||
				this.job.status === JobStatus.Stopped
			);
		}

		public get CanConfigurePit(): boolean {
			return (
				(this.job === undefined || this.job.status === JobStatus.Stopped) &&
				(this.pit !== undefined && this.pit.pitUrl !== undefined && this.pit.pitUrl.length > 0)
			);
		}

		public get CanViewFaults(): boolean {
			return !_.isUndefined(this.job);
		}

		public get CanViewMetrics(): boolean {
			return !_.isUndefined(this.job) && this.job.hasMetrics;
		}

		public OnSelectPit() {
			var modal = this.$modal.open({
				templateUrl: "html/modal/PitLibrary.html",
				controller: PitLibraryController
			});
			modal.result.then(() => {
				if (!this.pitService.IsConfigured) {
					this.$location.path('/quickstart/intro');
				}
			});
		}

		private get isJobRunning() {
			return (
				!(_.isUndefined(this.job)) &&
				this.job.status !== JobStatus.Stopped
			);
		}
	}
}
