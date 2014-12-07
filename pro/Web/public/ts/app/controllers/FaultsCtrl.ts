/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class FaultsController {

		static $inject = [
			"$scope",
			"$routeParams",
			"JobService",
			"FaultDetailResource"
		];

		constructor(
			$scope: IViewModelScope,
			$routeParams: ng.route.IRouteParamsService,
			private jobService: Services.JobService,
			private faultDetailResource: Models.IFaultDetailResource
		) {
			$scope.vm = this;
			var bucket = $routeParams['bucket'];
			if (bucket === "all") {
				this.GridFaults.data = "vm.Faults";
				this.Title = "All Faults";
			} else {
				this.Title = "Faults For " + bucket;
				this.GridFaults.data = "vm.BucketFaults";
				this.BucketFaults = _.filter(this.jobService.Faults, (fault: Models.IFaultSummary) => {
					return bucket === (fault.majorHash + '_' + fault.minorHash);
				});
			}
		}

		public get Job(): Models.IJob {
			return this.jobService.Job;
		}

		public get Faults(): Models.IFaultSummary[] {
			return this.jobService.Faults;
		}

		public get IsFaultSelected(): boolean {
			return !_.isUndefined(this.CurrentFault);
		}

		public Title: string = "All Faults";

		public BucketFaults: Models.IFaultSummary[];

		public CurrentFault: Models.IFaultDetail;

		public IsDetailActive: boolean;

		public GridFaults: ngGrid.IGridOptions = {
			data: "vm.Faults",
			sortInfo: { fields: ["iteration"], directions: ["asc"] },
			columnDefs: [
				{ field: "iteration", displayName: "#" },
				{ field: "timeStamp", displayName: "When", cellFilter: "date:'M/d/yy h:mm a'" },
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
			afterSelectionChange: (rowItem?: ngGrid.IRow) => {
				if (!rowItem.selected) {
					// ignore 'deselected' events
					return;
				}
				var fault = <Models.IFaultSummary> rowItem.entity;
				var promise = this.faultDetailResource.get({ id: ExtractId('faults', fault.faultUrl) });
				promise.$promise.then((detail: Models.IFaultDetail) => {
					this.CurrentFault = detail;
					this.IsDetailActive = true;
				});
			},
			keepLastSelected: true,
			plugins: [new ngGridFlexibleHeightPlugin({ minHeight: 500 })]
		};
	}
}
