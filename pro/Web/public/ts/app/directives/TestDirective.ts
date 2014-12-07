/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class TestDirective implements ng.IDirective {
		public restrict = 'E';
		public templateUrl = 'html/directives/test.html';
		public controller = 'TestController';
		public scope = {};
	}

	export class TestController {
		static $inject = [
			"$scope",
			"TestService"
		];

		constructor(
			private $scope: IAgentScope,
			private testService: Services.TestService
		) {
			$scope.vm = this;
		}

		public get TestEvents(): Models.ITestEvent[]{
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

		public StatusClass(row: Models.ITestEvent): any {
			return {
				'icon-ok green': row.status === 'pass',
				'icon-warning-sign orange': row.status === 'warn',
				'icon-remove red': row.status === 'fail'
			};
		}
	}
}
