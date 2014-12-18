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
			private testService: TestService,
			private wizardService: WizardService
		) {
			$scope.vm = this;
			if ($location.path() === '/quickstart/test') {
				this.wizardService.GetTrack('test').Begin();
			}
		}

		public IsComplete(step: string) {
			return this.wizardService.GetTrack(step).isComplete;
		}

		public get CanWizardBeginTest(): boolean {
			return this.CanBeginTest && this.IsComplete('fault');
		}

		public get CanBeginTest(): boolean {
			return this.testService.CanBeginTest;
		}

		public get CanContinue() {
			return this.testService.TestResult.status === TestStatus.Pass;
		}

		public get ShowTestPass(): boolean {
			return this.testService.TestResult.status === TestStatus.Pass;
		}

		public get ShowTestFail() {
			return this.testService.TestResult.status === TestStatus.Fail;
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
