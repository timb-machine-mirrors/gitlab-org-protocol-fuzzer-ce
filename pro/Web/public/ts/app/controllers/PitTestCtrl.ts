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

		public get TestEvents(): Models.ITestEvent[] {
			return this.testService.TestResult.events;
		}

		public get TestStatus(): string {
			return this.testService.TestResult.status;
		}

		public get TestLog(): string {
			return this.testService.TestResult.log;
		}

		public get TestTime(): string {
			return this.testService.TestTime;
		}

		public Tabs: ITab[] = [
			{ title: "Summary", content: "html/tabs/test-grid.html", active: true, disabled: false },
			{ title: "Log", content: "html/tabs/test-raw.html", active: false, disabled: false }
		];

		public Grid: ngGrid.IGridOptions = {
			data: "vm.TestEvents",
			columnDefs: [
				{
					field: "status",
					displayName: " ",
					width: 25,
					cellTemplate: "html/grid/test/status.html"
				},
				{
					displayName: "Message",
					cellTemplate: "html/grid/test/message.html"
				}
			],
			rowHeight: 45,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

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
