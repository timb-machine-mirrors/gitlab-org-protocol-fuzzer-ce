/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />

module DashApp {

	import P = Models.Peach;

	export class DashController {
		"use strict";

		private pitConfigSvc: Services.IPitConfiguratorService;

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
		
		static $inject = ["$scope", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, pitConfiguratorService: Services.IPitConfiguratorService) {
			this.pitConfigSvc = pitConfiguratorService;
			$scope.vm = this;
		}
	}
}