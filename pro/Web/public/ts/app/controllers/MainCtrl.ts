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
			private pitService: Services.PitService,
			private jobService: Services.JobService,
			private wizardService: Services.WizardService
		) {
			$scope.vm = this;

			var pitId = $location.search()['pit'];
			if (pitId) {
				this.pitService.SelectPit('/p/pits/' + pitId);
			} else if (!this.pitService.Pit) {
				this.OnSelectPit();
			}

			//this.peachSvc.GetJobs((job: Models.IJob) => {
			//	if (job) {
			//		this.pitConfigSvc.Job = job;
			//	}
			//	else {
			//		this.showPitSelector();
			//	}
			//});
		}

		private get pit(): Models.IPit {
			return this.pitService.Pit;
		}

		private get job(): Models.IJob {
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
				this.job.status === Models.JobStatus.Stopped
			);
		}

		public get CanConfigurePit() {
			return (
				(this.job === undefined || this.job.status === Models.JobStatus.Stopped) &&
				(this.pit !== undefined && this.pit.pitUrl !== undefined && this.pit.pitUrl.length > 0)
			);
		}

		public get CanViewFaults() {
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
				this.job.status !== Models.JobStatus.Stopped
			);
		}
	}
}
