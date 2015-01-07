/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class MainController {

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$window,
			Constants.Angular.$location,
			Constants.Angular.$modal,
			Constants.Services.Pit,
			Constants.Services.Test,
			Constants.Services.Job,
			Constants.Services.Wizard
		];

		constructor(
			$scope: IViewModelScope,
			private $window: ng.IWindowService,
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
					var pitId = $location.search()['pit'] || $window.sessionStorage.getItem('pitId');
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

		public get CanSelectPit(): boolean {
			return !this.testService.IsPending
				&& !this.jobService.IsRunning
				&& !this.jobService.IsPaused;
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
			if (this.jobService.IsRunning || this.testService.IsPending) {
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
				templateUrl: "html/modal/PitLibrary.html",
				controller: PitLibraryController
			});
			modal.result.then(() => {
				if (!this.pitService.IsConfigured) {
					this.$location.path('/quickstart/intro');
				} else {
					this.$location.path('/');
				}
			});
		}
	}
}
