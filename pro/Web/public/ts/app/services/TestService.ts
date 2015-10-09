/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var TEST_INTERVAL = 1000;

	export class TestService {

		static $inject = [
			C.Angular.$rootScope,
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$timeout,
			C.Services.Pit
		];

		constructor(
			private $rootScope: ng.IRootScopeService,
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $timeout: ng.ITimeoutService,
			private pitService: PitService
		) {
			$rootScope.$on(C.Events.PitChanged,() => {
				this.reset();
			});
		}

		private pendingResult: ng.IDeferred<any>;
		private isPending: boolean = false;
		private testResult: ITestResult;
		private testTime: string;

		public get IsPending(): boolean {
			return this.isPending;
		}

		public get TestResult(): ITestResult {
			return this.testResult;
		}

		public get TestTime(): string {
			return this.testTime;
		}

		public get CanBeginTest(): boolean {
			return !this.isPending;
		}

		public get IsAvailable(): boolean {
			return !_.isEmpty(this.testTime);
		}

		public BeginTest(): ng.IPromise<any> {
			this.reset();

			this.pendingResult = this.$q.defer<any>();
			this.isPending = true;

			this.testTime = moment().format("h:mm a");

			var request: IJobRequest = {
				pitUrl: this.pitService.Pit.pitUrl,
				dryRun: true
			};

			this.$http.post(C.Api.Jobs, request)
				.success((job: IJob) => {
					this.onPoll(job);
				})
				.catch((response: ng.IHttpPromiseCallbackArg<IError>) => {
					if (response.status === 403) {
						this.setFailure('Peach was unable to start the test. Please make sure another there are no other running tests or jobs and try again.');
					} else if (response.status === 404) {
						this.setFailure('Peach was unable to start the test. Please make sure the pit exists and try again.');
					} else {
						this.setFailure(response.data.errorMessage);
					}
					this.pendingResult.reject();
				})
			;

			return this.pendingResult.promise;
		}

		private reset(): void {
			this.testTime = "";
			this.testResult = {
				status: "",
				log: "",
				events: []
			};
		}

		private onPoll(job: IJob): void {
			this.$http.get(job.firstNodeUrl)
				.success((data: ITestResult) => {
					this.testResult = data;
					if (data.status === TestStatus.Active) {
						this.$timeout(() => { this.onPoll(job); }, TEST_INTERVAL);
					} else {
						if (data.status === TestStatus.Pass) {
							this.pendingResult.resolve();
						} else {
							this.pendingResult.reject();
						}
						this.isPending = false;
						this.$http.delete(job.jobUrl);
					}
				})
				.catch((response: ng.IHttpPromiseCallbackArg<IError>) => {
					this.setFailure(response.data.errorMessage);
					this.pendingResult.reject();
				})
			;
		}

		private setFailure(reason): void {
			this.isPending = false;
			this.testResult.status = TestStatus.Fail;

			var event: ITestEvent = {
				id: this.testResult.events.length + 1,
				status: TestStatus.Fail,
				description: 'Test execution failure.',
				resolve: reason
			};

			this.testResult.events.push(event);
		}
	}
}
