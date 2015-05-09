/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class JobsController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private jobService: JobService
		) {
			$scope.vm = this;

			this.jobService.GetJobs();
		}

		public get Jobs(): IJob[]{
			return this.jobService.Jobs;
		}

		public OnJobSelected(job: IJob) {
			var params = {
				job: 1
			};
			this.$state.go(C.States.JobDashboard, params);
		}
	}
}
