/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />

module DashApp {
	"use strict";

	import P = DashApp.Models.Peach;

	export class FaultsController {
		private pitConfigSvc: Services.IPitConfiguratorService;
		private peachSvc: Services.IPeachService;

		public get job(): P.Job {
			return this.pitConfigSvc.Job;
		}
		public faults: P.Fault[] = [];
		
		public tabs: ITab[] = [
			{ title: "Summary", content: "../partials/fault-summary.html", active: true, disabled: false },
			{ title: "Detail", content: "../partials/fault-detail.html", active: false, disabled: false }
		];

		public gridFaults: ngGrid.IGridOptions = {
			showGroupPanel: true,
			jqueryUIDraggable: true,
			data: "vm.faults",
			sortInfo: { fields: ["iteration"], directions: ["asc"] },
			columnDefs: [
				{ field: "iteration", displayName: "#" },
				{
					field: "timeStamp", displayName: "When", cellFilter: "date:'M/d/yy h:mma'"
				},
				{ field: "source", displayName: "Monitor" },
				{ field: "exploitability", displayName: "Risk" },
				{ field: "majorHash", displayName: "Major Hash" },
				{ field: "minorHash", displayName: "Minor Hash" }
			],
			enablePaging: true,
			totalServerItems: "vm.job.faultCount",
			showFooter: true,
			multiSelect: false,
			afterSelectionChange: (r, e) => {
				this.peachSvc.GetSingleThing((<P.Fault>r.entity).faultUrl).get((data) => {
					this.currentFault = data;
					this.tabs[1].active = true;
				});
			}
		};

		public currentFault: P.Fault;

		public pagingOptions: ngGrid.IPagingOptions = {
			pageSize: 10,
			currentPage: 1,
			pageSizes: [10, 20, 50]
		};

		public setPagingData(page, pageSize) {
			var pagedFaults = this.pitConfigSvc.Faults.slice((page - 1) * pageSize, page * pageSize);
			this.faults = pagedFaults;
		}


		static $inject = ["$scope", "poller", "pitConfiguratorService", "peachService"];

		constructor($scope: ViewModelScope, poller, pitConfiguratorService: Services.IPitConfiguratorService, peachService: Services.IPeachService) {
			$scope.vm = this;
			this.pitConfigSvc = pitConfiguratorService;
			this.peachSvc = peachService;

			this.gridFaults.pagingOptions = this.pagingOptions;
		
			this.setPagingData(this.pagingOptions.currentPage, this.pagingOptions.pageSize);

			$scope.$watch("vm.pagingOptions", (newVal:ngGrid.IPagingOptions, oldVal:ngGrid.IPagingOptions) => {
				if (newVal !== oldVal && newVal.currentPage !== oldVal.currentPage) {
					this.setPagingData(this.pagingOptions.currentPage, this.pagingOptions.pageSize);
				}					
			});

			$scope.$watch("vm.job.faultCount", (newVal: number, oldVal: number) => {
				if (newVal !== oldVal) {
					this.setPagingData(this.pagingOptions.currentPage, this.pagingOptions.pageSize);
				}
			});
		}
	}

	export interface ITab {
		title: string;
		content: string;
		active: boolean;
		disabled: boolean;
	}
}