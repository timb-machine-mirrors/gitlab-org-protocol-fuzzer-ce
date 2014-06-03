/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />

module DashApp {

	import P = Models.Peach;

	export class DashController {
		"use strict";


		public pit: P.Pit;
		public faults: P.Fault[];
		
		static $inject = ["$scope", "poller", "peachService"];

		constructor($scope, poller, peachService: Services.IPeachService) {
			
			peachService.GetPit(1, (data: P.Pit) => {
				$scope.pit = data;
			});


			var jobResource = peachService.GetJob();

			var jobPoller = poller.get(jobResource);

			jobPoller.promise.then(null, null, function (data) {
				$scope.job = data;
			});
		}
	}
}