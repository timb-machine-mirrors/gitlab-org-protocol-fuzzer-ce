/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var JobsDirective: IDirective = {
		ComponentID: C.Directives.Jobs,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Jobs,
		controller: C.Controllers.Jobs,
		scope: {
			limit: '='
		}
	}

	export interface IJobsDirectiveScope extends IViewModelScope {
		limit?: number;
	}

	export class JobsDirectiveController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Angular.$window,
			C.Services.Job
		];

		constructor(
			private $scope: IJobsDirectiveScope,
			private $state: ng.ui.IStateService,
			private $window: ng.IWindowService,
			private jobService: JobService
		) {
			$scope.vm = this;
			this.jobService.GetJobs()
				.then((jobs: IJob[]) => {
					if (this.$scope.limit) {
						this.AllJobs = _.last(jobs, $scope.limit);
					} else {
						this.AllJobs = jobs;
					}
				});
		}

		public Jobs: IJob[] = [];
		public AllJobs: IJob[] = [];

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
