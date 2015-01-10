/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class PitTestController {

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$location,
			Constants.Services.Pit,
			Constants.Services.Test,
			Constants.Services.Wizard
		];

		constructor(
			$scope: IViewModelScope,
			private $location: ng.ILocationService,
			private pitService: PitService,
			private testService: TestService,
			private wizardService: WizardService
		) {
			$scope.vm = this;
			if ($location.path() === Constants.Routes.WizardPrefix + Constants.Tracks.Test) {
				this.wizardService.GetTrack(Constants.Tracks.Test).Begin();
			}
		}

		public IsComplete(step: string) {
			return this.wizardService.GetTrack(step).isComplete;
		}

		public get CanWizardBeginTest(): boolean {
			return this.CanBeginTest && this.IsComplete(Constants.Tracks.Fault);
		}

		public get CanBeginTest(): boolean {
			return this.testService.CanBeginTest;
		}

		public get CanContinue() {
			return this.testService.TestResult.status === TestStatus.Pass;
		}

		public get ShowNotConfigured(): boolean {
			return !this.pitService.IsConfigured;
		}

		public get ShowTestPending(): boolean {
			return this.testService.IsPending;
		}

		public get ShowTestPass(): boolean {
			return this.testService.TestResult.status === TestStatus.Pass;
		}

		public get ShowTestFail() {
			return this.testService.TestResult.status === TestStatus.Fail;
		}

		public OnBeginTest() {
			this.wizardService.GetTrack(Constants.Tracks.Test).isComplete = false;
			this.testService.BeginTest();
		}

		public OnNextTrack() {
			this.wizardService.GetTrack(Constants.Tracks.Test).isComplete = true;
			this.$location.path(Constants.Routes.WizardPrefix + Constants.Tracks.Done);
		}

		public OnDashboard() {
			this.$location.path(Constants.Routes.Home);
		}
	}
}
