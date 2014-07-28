/// <reference path="../../../Scripts/typings/moment/moment.d.ts" />
/// <reference path="../Services/pitconfigurator.ts" />
/// <reference path="main.ts" />


module DashApp {
	"use strict";

	declare function ngGridFlexibleHeightPlugin(opts?: any): void; 

	export class DashController {

		private location: ng.ILocationService;
		private pitConfigSvc: Services.IPitConfiguratorService;

		public get pit(): Models.Pit {
			if (this.pitConfigSvc != undefined && this.pitConfigSvc.Pit != undefined) {
				return this.pitConfigSvc.Pit;
			}
			else {
				return undefined;
			}
		}

		public get job(): Models.Job {

			if (this.pitConfigSvc != undefined && this.pitConfigSvc.Job != undefined) {
				return this.pitConfigSvc.Job;
			}
			else {
				return undefined;
			}
		}

		public get runtimeString(): string {
			if (this.pitConfigSvc.Job == undefined)
				return "";
			else
				return moment(new Date(0, 0, 0, 0, 0, this.pitConfigSvc.Job.runtime)).format("H:mm:ss");
		}

		public get CanStart() {
			return (this.pitConfigSvc.CanStartJob || this.pitConfigSvc.CanContinueJob);
		}

		public get CanPause() {
			return this.pitConfigSvc.CanPauseJob;
		}

		public get CanStop() {
			return this.pitConfigSvc.CanStopJob;
		}

		public Start() {
			this.pitConfigSvc.StartJob();
		}

		public Pause() {
			this.pitConfigSvc.PauseJob();
		}

		public Stop() {
			this.pitConfigSvc.StopJob();
		}


		public gridRecentFaults = {
			data: "vm.recentFaults",
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
			totalServerItems: "vm.job.faultCount",
			showFooter: false,
			plugins: [new ngGridFlexibleHeightPlugin({ minHeight: 300 })]
		}; 
		
		public recentFaults: Models.Fault[] = [];

		static $inject = ["$scope", "pitConfiguratorService", "$location"];

		constructor($scope: ViewModelScope, pitConfiguratorService: Services.IPitConfiguratorService, $location: ng.ILocationService) {
			this.pitConfigSvc = pitConfiguratorService;
			this.location = $location;
			$scope.vm = this;

			$scope.$watch("vm.job.faultCount", () => {
				this.recentFaults = this.pitConfigSvc.Faults.sort((a: Models.Fault, b: Models.Fault) => {
					if (a.iteration == b.iteration) { return 0; }
					else if (a.iteration > b.iteration) { return -1; }
					else { return 1;}
				}).slice(0, 9);
			});
		}
	}
}