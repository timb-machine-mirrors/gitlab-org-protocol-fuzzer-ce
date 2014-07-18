/// <reference path="../../../Scripts/typings/ng-grid/ng-grid.d.ts" />
/// <reference path="../Models/models.ts" />
/// <reference path="../Services/pitconfigurator.ts" />
/// <reference path="../Services/peach.ts" />
/// <reference path="main.ts" />

module DashApp {
	"use strict";

	
	declare function ngGridFlexibleHeightPlugin(opts?: any): void; 

	export class FaultsController {
		private pitConfigSvc: Services.IPitConfiguratorService;
		private peachSvc: Services.IPeachService;

		public get job(): Models.Job {
			return this.pitConfigSvc.Job;
		}
		//public faults: Models.Fault[] = [];
		public get faults(): Models.Fault[] {
			return this.pitConfigSvc.Faults;
		}
		
		public tabs: ITab[] = [
			{ title: "Summary", content: "../partials/fault-summary.html", active: true, disabled: false },
			{ title: "Detail", content: "../partials/fault-detail.html", active: false, disabled: false }
		];

		public gridFaults: ngGrid.IGridOptions = {
			data: "vm.faults",
			sortInfo: { fields: ["iteration"], directions: ["asc"] },
			columnDefs: [
				{ field: "iteration", displayName: "#" },
				{	field: "timeStamp", displayName: "When", cellFilter: "date:'M/d/yy h:mma'" },
				{ field: "source", displayName: "Monitor" },
				{ field: "exploitability", displayName: "Risk" },
				{ field: "majorHash", displayName: "Major Hash" },
				{ field: "minorHash", displayName: "Minor Hash" }
			],
			totalServerItems: "vm.job.faultCount",
			multiSelect: false,
			afterSelectionChange: (r, e) => {
				this.peachSvc.GetFault((<Models.Fault>r.entity).faultUrl,(data: Models.Fault) => {
					this.currentFault = data;
					this.tabs[1].active = true;
				});
			},
			keepLastSelected: true
		};
		
		/*
			showGroupPanel: true,
			jqueryUIDraggable: true,
			showFooter: false,
			enablePaging: false,
			plugins: [new ngGridFlexibleHeightPlugin({ minHeight: 200 })]
		//*/		

		public currentFault: Models.Fault;

		public get isFaultSelected(): boolean {
			return (this.currentFault != undefined);
		}

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

			this.initializeGrid($scope);
		}

		private initializeGrid(scope: ViewModelScope) {
			//this.gridFaults.pagingOptions = this.pagingOptions;

			//this.setPagingData(this.pagingOptions.currentPage, this.pagingOptions.pageSize);

			//scope.$watch("vm.pagingOptions", (newVal: ngGrid.IPagingOptions, oldVal: ngGrid.IPagingOptions) => {
			//	if (newVal !== oldVal && newVal.currentPage !== oldVal.currentPage) {
			//		this.setPagingData(this.pagingOptions.currentPage, this.pagingOptions.pageSize);
			//	}
			//});

			//scope.$watch("vm.job.faultCount", (newVal: number, oldVal: number) => {
			//	if (newVal !== oldVal) {
			//		this.setPagingData(this.pagingOptions.currentPage, this.pagingOptions.pageSize);
			//	}
			//});
		}
	}

	export interface ITab {
		title: string;
		content: string;
		active: boolean;
		disabled: boolean;
	}
}