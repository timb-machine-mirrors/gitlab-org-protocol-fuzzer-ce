/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../Scripts/typings/angular-ui-bootstrap/angular-ui-bootstrap.d.ts" />

module DashApp {
	"use strict";

	import P = Models.Peach;

	export class MainController {
		private peachSvc: Services.IPeachService;
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

		public get jobRunningTooltip(): string {
			if (this.job == undefined)
				return "";
			else
				return "Disabled while running a Job";
		}

		public get jobNotRunningTooltip(): string {
			if (this.job == undefined)
				return "Disabled while not running a Job";
			else
				return "";
		}


		public location: ng.ILocationService;
		
		//#endregion


		static $inject = ["$scope", "$resource", "$location", "$modal", "poller", "peachService", "pitConfiguratorService","$http"];

		constructor($scope: ViewModelScope, $resource, $location: ng.ILocationService, $modal: ng.ui.bootstrap.IModalService, poller, peachService: Services.IPeachService, pitConfiguratorService: Services.IPitConfiguratorService, $http: ng.IHttpService) {
			$scope.vm = this;

			this.modal = $modal;
			this.peachSvc = peachService;
			this.location = $location;
			this.pitConfigSvc = pitConfiguratorService;
			this.poller = poller;


			this.initialize();
		}

		private initialize() {
			this.peachSvc.GetJobs((data: P.Job[]) => {
				if (data.length > 0) {
					this.pitConfigSvc.Job = data[0]; 
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
				backdrop: "static",
				controller: PitLibraryController,
				resolve: {
					peachsvc: () => {
						return this.peachSvc;
					}
				}
			})
			.result.then((pitUrl: string) => {
				this.peachSvc.GetPit(pitUrl, (data: P.Pit) =>
				{
					if (data.locked) {
						this.showPitCopier(data);
					}
					else {
						this.pitConfigSvc.Pit = data;
					}
				});
			});
		}

		public showPitCopier(pit: P.Pit) {
			this.modal.open({
				templateUrl: "../partials/copy-pit.html",
				keyboard: false,
				backdrop: "static",
				controller: CopyPitController,
				resolve: {
					pit: () => {
						return pit;
					},
					libraryUrl: () => {
						return this.pitConfigSvc.UserPitLibrary;
					},
					peachSvc: () => {
						return this.peachSvc;
					}
				}
			}).result.then((pit: P.Pit) => {
				this.pitConfigSvc.Pit = pit;
			});
		}
	}

	export interface ViewModelScope extends ng.IScope {
		vm: any;
	}
}