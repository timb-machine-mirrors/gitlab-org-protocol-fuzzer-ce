/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../Scripts/typings/moment/moment.d.ts" />
/// <reference path="../../../Scripts/typings/ng-grid/ng-grid.d.ts" />

module DashApp {
	"use strict";

	import P = Models.Peach;

	export class DashController {
		"use strict";


		private pitConfigSvc: Services.IPitConfiguratorService;

		public get pit(): P.Pit {
			if (this.pitConfigSvc != undefined && this.pitConfigSvc.Pit != undefined) {
				return this.pitConfigSvc.Pit;
			}
			else {
				return undefined;
			}
		}

		public get job(): P.Job {

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

		//showGroupPanel: true,
		//	jqueryUIDraggable: true,

		public gridRecentFaults = {
			data: "vm.recentFaults",
			sortInfo: { fields: ["iteration"], directions: ["desc"] },
			columnDefs: [
				{ field: "iteration", displayName: "#" },
				{ field: "timestamp", displayName: "When" },
				{ field: "source", displayName: "Monitor" },
				{ field: "exploitability", displayName: "Risk" },
				{ field: "majorHash", displayName: "Major Hash" },
				{ field: "minorHash", displayName: "Minor Hash" }
			],
			enablePaging: true,
			pagingOptions: { pageSize: 10, currentPage: 1, pageSizes: [10] },
			totalServerItems: "vm.job.faultCount",
			showFooter: false
		};
		
		public recentFaults: P.Fault[] = [];

		static $inject = ["$scope", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, pitConfiguratorService: Services.IPitConfiguratorService) {
			this.pitConfigSvc = pitConfiguratorService;
			$scope.vm = this;

			$scope.$watch("vm.job.faultCount", () => {
				this.recentFaults = this.pitConfigSvc.Faults.sort((a: P.Fault, b: P.Fault) => {
					if (a.iteration == b.iteration) { return 0; }
					else if (a.iteration > b.iteration) { return -1; }
					else { return 1;}
				}).slice(0, 9);
			});
		}
	}
}