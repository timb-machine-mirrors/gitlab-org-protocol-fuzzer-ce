/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class JobsController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job,
			C.Angular.$window
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private jobService: JobService,
			private $window: ng.IWindowService
		) {
			this.jobService.GetJobs();
		}

		public Jobs: IJob[] = [];
		public get AllJobs(): IJob[] {
			return this.jobService.Jobs;
		}

		public OnJobSelected(job: IJob) {
			this.$state.go(C.States.Job, { job: job.id });
		}

		public IsReportDisabled(job: IJob): boolean {
			return !_.isString(job.reportUrl);
		}

		public IsRemoveDisabled(job: IJob): boolean {
			return job.status !== JobStatus.Stopped;
		}

		public OnRemove($event: ng.IAngularEvent, job: IJob) {
			$event.preventDefault();
			$event.stopPropagation();
			this.jobService.Delete(job);
		}

		public OnViewReport($event: ng.IAngularEvent, job: IJob) {
			$event.preventDefault();
			$event.stopPropagation();
			this.$window.open(job.reportUrl);
		}
	}
}
