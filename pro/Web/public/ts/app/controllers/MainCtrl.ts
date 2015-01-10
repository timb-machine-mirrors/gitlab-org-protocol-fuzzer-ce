/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class MainController {

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$location,
			Constants.Angular.$modal,
			Constants.Services.Pit,
			Constants.Services.Test,
			Constants.Services.Job,
			Constants.Services.Wizard
		];

		constructor(
			$scope: IViewModelScope,
			private $location: ng.ILocationService,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService,
			private testService: TestService,
			private jobService: JobService,
			private wizardService: WizardService
		) {
			$scope.vm = this;

			var promise = this.jobService.GetJobs();
			promise.then(() => {
				if (_.isUndefined(this.job)) {
					if (!this.pitService.RestorePit()) {
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

		public get SelectPitPrompt(): string {
			return _.isUndefined(this.pit) ? "Select a Pit" : this.pit.name;
		}

		public get FaultCount(): any {
			var count = 0;
			if (this.job) {
				count = this.job.faultCount;
			}
			return count || '';
		}

		public get JobRunningTooltip(): string {
			if (!this.CanSelectPit) {
				return "Disabled while running a Job or a Test";
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
			if (this.jobService.IsRunning && !this.job.hasMetrics) {
				return "Metrics unavailable for this Job.";
			}
			return "";
		}

		public IsComplete(step: string) {
			return this.wizardService.GetTrack(step).isComplete;
		}

		public get CanSelectPit(): boolean {
			return !this.testService.IsPending
				&& !this.jobService.IsRunning
				&& !this.jobService.IsPaused;
		}

		public get CanConfigurePit(): boolean {
			return (
				(_.isUndefined(this.job) || this.job.status === JobStatus.Stopped) &&
				(!_.isUndefined(this.pit) && !_.isEmpty(this.pit.pitUrl) && this.pit.pitUrl.length > 0)
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
				templateUrl: Constants.Templates.Modal.PitLibrary,
				controller: PitLibraryController
			});
			modal.result.then(() => {
				if (!this.pitService.IsConfigured) {
					this.$location.path(Constants.Routes.WizardPrefix + Constants.Tracks.Intro);
				} else {
					this.$location.path(Constants.Routes.Home);
				}
			});
		}
	}
}
