/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class FaultsController {

		static $inject = [
			"$scope",
			"$routeParams",
			"PitService",
			"JobService"
		];

		constructor(
			$scope: IViewModelScope,
			$routeParams: ng.route.IRouteParamsService,
			private pitService: Services.PitService,
			private jobService: Services.JobService
		) {
			$scope.vm = this;
			var bucket = $routeParams['bucket'];
			if (bucket === "all") {
				this.GridFaults.data = "vm.Faults";
				this.Title = "All Faults";
			} else {
				this.Title = "Faults For " + bucket;
				this.GridFaults.data = "vm.BucketFaults";
				//this.peachSvc.GetJobFaults(this.pitConfigSvc.Job.jobUrl, (faults: Models.IFault[]) => {
				//	this.bucketFaults = $.grep(faults, (e) => {
				//		return this.bucket === e.majorHash + '_' + e.minorHash;
				//	});
				//});
			}
		}

		public get Job(): Models.IJob {
			return this.jobService.Job;
		}

		public get Faults(): Models.IFault[] {
			return this.jobService.Faults;
		}

		public Title: string = "All Faults";

		public BucketFaults: Models.IFault[];

		public Tabs: ITab[] = [
			{ title: "Summary", content: "html/fault/summary.html", active: true, disabled: false },
			{ title: "Detail", content: "html/fault/detail.html", active: false, disabled: false }
		];

		public GridFaults: ngGrid.IGridOptions = {
			data: "vm.faults",
			sortInfo: { fields: ["iteration"], directions: ["asc"] },
			columnDefs: [
				{ field: "iteration", displayName: "#" },
				{ field: "timeStamp", displayName: "When", cellFilter: "date:'M/d/yy h:mma'" },
				{ field: "source", displayName: "Monitor" },
				{ field: "exploitability", displayName: "Risk" },
				{ field: "majorHash", displayName: "Major Hash" },
				{ field: "minorHash", displayName: "Minor Hash" },
				{
					field: "faultUrl",
					displayName: "Download",
					cellTemplate: "html/grid/faults/download.html"
				}
			],
			totalServerItems: "vm.job.faultCount",
			multiSelect: false,
			afterSelectionChange: (r, e) => {
				//this.peachSvc.GetFault((<Models.IFault>r.entity).faultUrl, (data: Models.IFault) => {
				//	this.CurrentFault = data;
				//	this.Tabs[1].active = true;
				//});
			},
			keepLastSelected: true
		};

		public CurrentFault: Models.IFault;

		public get IsFaultSelected(): boolean {
			return !_.isUndefined(this.CurrentFault);
		}
	}
}
