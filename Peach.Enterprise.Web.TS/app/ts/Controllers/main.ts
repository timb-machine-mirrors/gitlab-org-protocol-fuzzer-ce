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
			if (this.IsJobRunning)
				return "Disabled while running a Job";
			else
				return "";
		}

		public get jobNotRunningTooltip(): string {
			if (this.IsJobRunning)
				return "Disabled while not running a Job";
			else
				return "";
		}

		public location: ng.ILocationService;

		public get IntroComplete() {
			return this.pitConfigSvc.IntroComplete;
		}

		public get SetVarsComplete() {
			return this.pitConfigSvc.SetVarsComplete;
		}

		public get FaultComplete() {
			return this.pitConfigSvc.FaultMonitorsComplete;
		}

		public get AutoComplete() {
			return this.pitConfigSvc.AutoMonitorsComplete;
		}

		public get DataComplete() {
			return this.pitConfigSvc.DataMonitorsComplete;
		}

		public get TestComplete() {
			return this.pitConfigSvc.TestComplete;
		}

		public get DoneComplete() {
			return this.pitConfigSvc.DoneComplete;
		}

		public get CanSelectPit() {
			if (this.job == undefined || this.job.status == P.JobStatuses.Stopped) {
				return true;
			}

			return false;
		}

		public get CanConfigurePit() {
			if ((this.job == undefined || this.job.status == P.JobStatuses.Stopped) && (this.pit != undefined && this.pit.pitUrl !== undefined && this.pit.pitUrl.length > 0)) {
				return true;
			}

			return false;
		}

		public get IsJobRunning() {
			if (this.job != undefined && this.job.status != P.JobStatuses.Stopped) {
				return true;
			}

			return false;
		}

		public get CanViewFaults() {
			return (this.job != undefined);
		}


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
			this.peachSvc.GetJob((job: P.Job) => {
				if (job != undefined) {
					this.pitConfigSvc.Job = job; 
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
						this.pitConfigSvc.Pit = new P.Pit(data);
						if (data.configured == false) {
							this.location.path("#/configurator/intro");
						}
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
				this.pitConfigSvc.Pit = new P.Pit(pit);
				if (pit.configured == false) {
					this.location.path("/configurator/intro");
				}
			});
		}
	}

	export interface ViewModelScope extends ng.IScope {
		vm: any;
	}
}