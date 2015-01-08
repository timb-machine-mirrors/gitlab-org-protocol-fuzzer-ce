/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var TestDirective: IDirective = {
		ComponentID: Constants.Directives.Test,
		restrict: 'E',
		templateUrl: 'html/directives/test.html',
		controller: Constants.Controllers.Test,
		scope: {}
	}

	export class TestController {
		static $inject = [
			Constants.Angular.$scope,
			Constants.Services.Test
		];

		constructor(
			private $scope: IAgentScope,
			private testService: TestService
		) {
			$scope.vm = this;
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

		public StatusClass(row: ITestEvent): any {
			return {
				'glyphicon glyphicon-ok green': row.status === TestStatus.Pass,
				'glyphicon glyphicon-remove red': row.status === TestStatus.Fail
			};
		}
	}
}
