 /// <reference path="../reference.ts" />

module Peach.Services {
	"use strict";

	export var JOB_INTERVAL = 500;

	export class JobService {
		static $inject = [
			"$q",
			"$http",
			"$interval",
			"PitService"
		];

		constructor(
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $interval: ng.IIntervalService,
			public pitService: PitService
		) {
		}

		private poller: ng.IPromise<any>;

		private job: Models.IJob;
		public get Job(): Models.IJob {
			return this.job;
		}

		private faults: Models.IFaultSummary[] = [];
		public get Faults(): Models.IFaultSummary[] {
			return this.faults;
		}

		public get CanStartJob(): boolean {
			return onlyIf(this.pitService.IsConfigured, () => 
				_.isUndefined(this.job) || this.job.status === Models.JobStatus.Stopped
			);
		}

		public get CanContinueJob(): boolean {
			return this.checkStatus([Models.JobStatus.Paused]);
		}

		public get CanPauseJob(): boolean {
			return this.checkStatus([Models.JobStatus.Running]);
		}

		public get CanStopJob(): boolean {
			return this.checkStatus([
				Models.JobStatus.Running,
				Models.JobStatus.Paused,
				Models.JobStatus.StartPending,
				Models.JobStatus.PausePending,
				Models.JobStatus.ContinuePending
			]);
		}

		public get RunningTime(): string {
			if (this.job === undefined) {
				return "";
			}
			return moment(new Date(0, 0, 0, 0, 0, this.job.runtime)).format("H:mm:ss");
		}

		public GetJobs(): ng.IPromise<void> {
			var deferred = this.$q.defer<void>();
			var promise = this.$http.get("/p/jobs");
			promise.success((jobs: Models.IJob[]) => {
				var hasPit = false;
				if (jobs.length > 0) {
					this.job = _.first(jobs);
					hasPit = !_.isEmpty(this.job.pitUrl);
					if (hasPit) {
						var promise2 = this.pitService.SelectPit(this.job.pitUrl);
						promise2.then(() => {
							deferred.resolve();
						});
					}
					this.startJobPoller();
				}
				if (!hasPit) {
					deferred.resolve();
				}
			});
			promise.error(reason => this.onError(reason));
			return deferred.promise;
		}

		public StartJob(job?: Models.IJob) {
			if (job === undefined) {
				job = { pitUrl: this.pitService.Pit.pitUrl };
			} else {
				job.pitUrl = this.pitService.Pit.pitUrl;
			}

			if (this.CanStartJob) {
				var promise = this.$http.post("/p/jobs", job);
				promise.success((newJob: Models.IJob) => {
					this.job = newJob;
					this.startJobPoller();
				});
				promise.error(reason => this.onError(reason));
			} else if (this.CanContinueJob) {
				this.job.status = Models.JobStatus.ActionPending;
				this.$http.get(this.job.jobUrl + "/continue").error(reason => this.onError(reason));
			}
		}

		public PauseJob() {
			if (this.CanPauseJob) {
				this.job.status = Models.JobStatus.ActionPending;
				this.$http.get(this.job.jobUrl + "/pause").error(reason => this.onError(reason));
			}
		}

		public StopJob() {
			if (this.CanStopJob) {
				this.job.status = Models.JobStatus.ActionPending;
				this.$http.get(this.job.jobUrl + "/stop").error(reason => this.onError(reason));
			}
		}

		private checkStatus(good: string[]): boolean {
			return (
				this.job &&
				_.contains(good, this.job.status) &&
				this.pitService.IsConfigured
			);
		}

		private onError(response) {
			alert("Peach is busy with another task.\n" +
				"Confirm that there aren't multiple browsers accessing the same instance of Peach.");
			this.job = undefined;
			this.$interval.cancel(this.poller);
		}

		private startJobPoller() {
			if (!_.isUndefined(this.poller)) {
				return;
			}

			this.poller = this.$interval(() => {
				var promise = this.$http.get(this.job.jobUrl);
				promise.success((job: Models.IJob) => {
					this.job = job;
					if (job.status === Models.JobStatus.Stopped) {
						this.$interval.cancel(this.poller);
						this.poller = undefined;
					}
					if (this.faults.length !== job.faultCount) {
						this.reloadFaults();
					}
				});
				promise.error(reason => this.onError(reason));
			}, JOB_INTERVAL);
		}

		private reloadFaults() {
			var promise = this.$http.get(this.job.faultsUrl);
			promise.success((faults: Models.IFaultSummary[]) => {
				this.faults = faults;
			});
			promise.error(reason => this.onError(reason));
		}
	}
}
