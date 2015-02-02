/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var TestDirective: IDirective = {
		ComponentID: C.Directives.Test,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Test,
		controller: C.Controllers.Test,
		controllerAs: 'vm',
		scope: {}
	}

	export class TestController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Test
		];

		constructor(
			private $scope: IAgentScope,
			private testService: TestService
		) {
		}

		public get IsAvailable(): boolean {
			return this.testService.IsAvailable;
		}

		public get TestEvents(): ITestEvent[] {
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

		public get ShowTestPending(): boolean {
			return this.testService.IsPending;
		}

		public get ShowTestPass(): boolean {
			return this.testService.TestResult.status === TestStatus.Pass;
		}

		public get ShowTestFail() {
			return this.testService.TestResult.status === TestStatus.Fail;
		}

		public StatusClass(row: ITestEvent): any {
			return {
				'glyphicon glyphicon-ok green': row.status === TestStatus.Pass,
				'glyphicon glyphicon-remove red': row.status === TestStatus.Fail
			};
		}
	}
}
