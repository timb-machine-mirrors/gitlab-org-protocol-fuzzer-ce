/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class DashboardController {
		public Faults: Models.IFaultSummary[] = [];

		static $inject = [
			"$scope",
			"$modal",
			"PitService",
			"JobService"
		];

		constructor(
			$scope: IViewModelScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: Services.PitService,
			private jobService: Services.JobService
		) {
			$scope.vm = this;

			$scope.$watch("vm.Job.faultCount", () => {
				this.Faults = this.jobService.Faults.sort(
				(a: Models.IFaultSummary, b: Models.IFaultSummary) => {
					if (a.iteration === b.iteration) {
						return 0;
					} else if (a.iteration > b.iteration) {
						return -1;
					} else {
						return 1;
					}
				}).slice(0, 9);
			});
		}

		public get IsConfigured(): boolean {
			return this.pitService.IsConfigured;
		}

		public get CanControlPit(): boolean {
			var pit = this.pitService.Pit;
			return pit && pit.pitUrl && pit.pitUrl.length > 0;
		}

		public get Job(): Models.IJob {
			return this.jobService.Job;
		}

		public get RunningTime(): string {
			return this.jobService.RunningTime;
		}

		public get CanStart() {
			return this.jobService.CanStartJob || this.jobService.CanContinueJob;
		}

		public get CanPause() {
			return this.jobService.CanPauseJob;
		}

		public get CanStop() {
			return this.jobService.CanStopJob;
		}

		public StartWithOptions() {
			this.$modal.open({
				templateUrl: "html/modal/StartJob.html",
				controller: StartJobController
			}).result.then((job: Models.IJob) => {
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

		public Grid = {
			data: "vm.Faults",
			sortInfo: { fields: ["iteration"], directions: ["desc"] },
			columnDefs: [
				{ field: "iteration", displayName: "#" },
				{ field: "timeStamp", displayName: "When", cellFilter: "date:'M/d/yy h:mma'" },
				{ field: "source", displayName: "Monitor" },
				{ field: "exploitability", displayName: "Risk" },
				{ field: "majorHash", displayName: "Major Hash" },
				{ field: "minorHash", displayName: "Minor Hash" }
			],
			enablePaging: true,
			pagingOptions: { pageSize: 10, currentPage: 1, pageSizes: [10] },
			totalServerItems: "vm.Job.faultCount",
			showFooter: false,
			plugins: [new ngGridFlexibleHeightPlugin({ minHeight: 303 })]
		};
	}
}
