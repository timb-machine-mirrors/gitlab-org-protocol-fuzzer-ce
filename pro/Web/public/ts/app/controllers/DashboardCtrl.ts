/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class DashboardController {

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
			$scope.$watch(() => jobService.Faults.length, (newVal, oldVal) => {
				if (newVal !== oldVal) {
					this.RefreshFaults();
				}
			});

			this.RefreshFaults();
		}

		public Faults: IFaultSummary[] = [];

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

		public get CanPause(): boolean {
			return this.jobService.CanPause;
		}

		public get CanContinue(): boolean {
			return this.jobService.CanContinue;
		}

		public get CanStop(): boolean {
			return this.jobService.CanStop;
		}

		public Pause() {
			this.jobService.Pause();
		}

		public Stop() {
			this.jobService.Stop();
		}
		
		public Continue() {
			this.jobService.Continue();
		}

		public get StatusClass(): any {
			if (!_.isUndefined(this.Job) && !_.isUndefined(this.Job.result)) {
				return 'alert-danger';
			}
			return 'alert-info';
		}

		public ValueAlt(value, alt) {
			return _.isUndefined(value) ? alt : value;
		}

		public OnFaultSelected(fault: IFaultSummary) {
			var params = {
				bucket: 'all',
				id: fault.iteration
			};
			this.$state.go(C.States.JobFaultsDetail, params);
		}

		private RefreshFaults() {
			if (this.jobService.Faults.length > 0)
				this.Faults = _.last(this.jobService.Faults, 10).reverse();
		}
	}
}
