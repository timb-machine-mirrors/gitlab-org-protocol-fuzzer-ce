/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class DashboardController {

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$modal,
			Constants.Services.Pit,
			Constants.Services.Job
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

		public get ShowReady(): boolean {
			return onlyIf([this.pitService.Pit, !this.jobService.Job], () =>
				this.pitService.IsConfigured && this.CanStart
			);
		}

		public get ShowNotConfigured(): boolean {
			return onlyIf(this.pitService.Pit, () => !this.pitService.IsConfigured);
		}

		public get ShowLimited(): boolean {
			return onlyIf(this.Job, () => _.isEmpty(this.Job.pitUrl));
		}

		public get ShowStatus(): boolean {
			return !_.isUndefined(this.Job);
		}

		public get JobStatus(): string {
			return onlyIf(this.Job, () => this.Job.status);
		}

		public get JobMode(): string {
			return this.Job.mode;
		}

		public get Job(): IJob {
			return this.jobService.Job;
		}

		public get RunningTime(): string {
			return this.jobService.RunningTime;
		}

		public get CanStart() {
			return this.jobService.CanStart || this.jobService.CanContinue;
		}

		public get CanPause() {
			return this.jobService.CanPause;
		}

		public get CanStop() {
			return this.jobService.CanStop;
		}

		public StartWithOptions() {
			this.$modal.open({
				templateUrl: Constants.Templates.Modal.StartJob,
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

		public get StatusClass(): any {
			if (!_.isUndefined(this.Job) && !_.isUndefined(this.Job.result)) {
				return { 'alert-danger': true };
			}
			return { 'alert-info': true };
		}

		public ValueAlt(value, alt) {
			if (_.isUndefined(value)) {
				return alt;
			}
			return value;
		}

		private refreshFaults() {
			this.Faults = _.last(this.jobService.Faults, 10).reverse();
		}
	}
}
