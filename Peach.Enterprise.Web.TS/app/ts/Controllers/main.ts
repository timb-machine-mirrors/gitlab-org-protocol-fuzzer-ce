/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../scripts/typings/angular-ui-bootstrap/angular-ui-bootstrap.d.ts" />

module DashApp {

	import P = Models.Peach;

	export class MainController {
		private peachService: Services.IPeachService;
		private modal: ng.ui.bootstrap.IModalService;
		private pitConfigSvc: Services.IPitConfiguratorService;
		private poller: any;

		//#region Public Properties
		public get pit(): P.Pit {
			if (this.pitConfigSvc != undefined && this.pitConfigSvc.Pit != undefined)
				return this.pitConfigSvc.Pit;
			else
				return undefined;
		}

		public get job(): P.Job {
			if (this.pitConfigSvc != undefined && this.pitConfigSvc.Job != undefined)
				return this.pitConfigSvc.Job;
			else
				return undefined;
		}

		public get disabledTooltip(): string {
			if (this.job == undefined)
				return "";
			else
				return "Disabled while running a Job";
		}

		public location: ng.ILocationService;
		
		//#endregion


		static $inject = ["$scope", "$resource", "$location", "$modal", "poller", "peachService", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, $resource, $location: ng.ILocationService, $modal: ng.ui.bootstrap.IModalService, poller, peachService: Services.IPeachService, pitConfiguratorService: Services.IPitConfiguratorService) {
			$scope.vm = this;

			this.modal = $modal;
			this.peachService = peachService;
			this.location = $location;
			this.pitConfigSvc = pitConfiguratorService;
			this.poller = poller;

			this.peachService.GetJobs((data: P.Job[]) => {
				if (data.length > 0) {
					this.pitConfigSvc.Job = new P.Job(data[0]);
				}
				else {
					this.showPitSelector();
				}
			});


		}

		public showPitSelector() {
			this.modal.open({
				templateUrl: "../partials/pitlibrary.html",
				keyboard: false,
				backdrop: 'static',
				controller: PitLibraryController,
				resolve: {
					peachsvc: () => {
						return this.peachService;
					}
				}
			})
			.result.then((pitUrl: string) => {
				this.peachService.GetPit(pitUrl, (data: P.Pit) =>
				{
					this.pitConfigSvc.Pit = data;
				});
			});
		}
	}

	export interface ViewModelScope extends ng.IScope {
		vm: any;
	}

	export class StorageStrings {
		static Pit = "pit";
		static FaultMonitors = "faultMonitors";
		static DataMonitors = "dataMonitors";
		static AutoMonitors = "autoMonitors";
		static PitDefines = "pitDefines";
	}
}