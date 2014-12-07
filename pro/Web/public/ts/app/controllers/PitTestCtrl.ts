/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class PitTestController {

		static $inject = [
			"$scope",
			"$location",
			"TestService",
			"WizardService"
		];

		constructor(
			$scope: IViewModelScope,
			private $location: ng.ILocationService,
			private testService: Services.TestService,
			private wizardService: Services.WizardService
		) {
			$scope.vm = this;
		}

		public IsComplete(step: string) {
			return this.wizardService.GetTrack(step).isComplete;
		}

		public get CanBeginTest(): boolean {
			return this.IsComplete('fault');
		}

		public OnBeginTest() {
			this.wizardService.GetTrack("test").isComplete = false;
			this.testService.BeginTest();
		}

		public OnSubmit() {
			this.wizardService.GetTrack("test").isComplete = true;
			this.$location.path("/quickstart/done");
		}
	}
}
