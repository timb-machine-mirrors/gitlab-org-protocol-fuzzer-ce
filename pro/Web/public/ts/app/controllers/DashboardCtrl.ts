/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class DashboardController {

		static $inject = [
			C.Angular.$scope,
			C.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			private jobService: JobService
		) {
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

		public ValueOr(value, alt) {
			return _.isUndefined(value) ? alt : value;
		}
	}
}
