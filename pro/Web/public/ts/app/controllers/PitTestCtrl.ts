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
			if ($location.path() === '/quickstart/test') {
				this.wizardService.GetTrack('test').Begin();
			}
		}

		public IsComplete(step: string) {
			return this.wizardService.GetTrack(step).isComplete;
		}

		public get CanBeginTest(): boolean {
			return this.IsComplete('fault');
		}

		public get ShowTestPass(): boolean {
			return this.testService.TestResult.status === 'pass';
		}

		public get ShowTestFail() {
			return this.testService.TestResult.status === 'fail';
		}

		public get CanContinue() {
			return this.testService.TestResult.status === 'pass';
		}

		public OnBeginTest() {
			this.wizardService.GetTrack("test").isComplete = false;
			this.testService.BeginTest();
		}

		public OnNextTrack() {
			this.wizardService.GetTrack("test").isComplete = true;
			this.$location.path("/quickstart/done");
		}
	}
}
