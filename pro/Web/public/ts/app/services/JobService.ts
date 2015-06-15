 /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var JOB_INTERVAL = 1000;
	
	export class JobService {
		static $inject = [
			C.Angular.$rootScope,
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$state,
			C.Angular.$interval,
			C.Services.Pit
		];

		constructor(
			private $rootScope: ng.IRootScopeService,
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $state: ng.ui.IStateService,
			private $interval: ng.IIntervalService
		) {
		}

		private jobs: IJob[] = [];
		private poller: ng.IPromise<any>;
		private job: IJob;
		private faults: IFaultSummary[] = [];
		private pending: ng.IPromise<IJob>;
		
		public OnEnter(id: string) {
			var url = C.Api.JobUrl.replace(':id', id);
			this.pending = this.$http.get(url)
				.then((response: ng.IHttpPromiseCallbackArg<IJob>) => {
					this.job = response.data;
					this.$rootScope['job'] = this.job;
					if (this.job.status !== JobStatus.Stopped) {
						this.startJobPoller();
					}
					if (this.job.faultCount > 0) {
						var deferred = this.$q.defer<IJob>();
						this.reloadFaults()
							.success(() => { deferred.resolve(this.job); })
							.error(reason => { deferred.reject(reason); })
							.finally(() => { this.pending = undefined; })
						;
						return deferred.promise;
					}
					return undefined;
				}, (response: ng.IHttpPromiseCallbackArg<IError>) => {
					this.$state.go(C.States.MainError, { message: response.data.errorMessage });
				})
			;
		}
		
		public OnExit() {
			this.stopJobPoller();
			this.job = undefined;
			this.$rootScope['job'] = undefined;
			this.faults = [];
		}
		
		public get Jobs(): IJob[]{
			return this.jobs;
		}
		
		public get Job(): IJob {
			return this.job;
		}

		public get Faults(): IFaultSummary[] {
			return this.faults;
		}

		public get IsRunning(): boolean {
			return this.Job && this.Job.status === JobStatus.Running;
		}

		public get IsPaused(): boolean {
			return this.Job && this.Job.status === JobStatus.Paused;
		}

		public get CanStart(): boolean {
			return _.isUndefined(this.Job) || this.Job.status === JobStatus.Stopped;
		}

		public get CanContinue(): boolean {
			return this.Job && this.Job.status === JobStatus.Paused;
		}

		public get CanPause(): boolean {
			return this.Job && this.Job.status === JobStatus.Running;
		}

		public get CanStop(): boolean {
			return this.Job && this.Job.status !== JobStatus.Stopped;
		}

		public get RunningTime(): string {
			if (_.isUndefined(this.Job)) {
				return undefined;
			}
			return moment(new Date(0, 0, 0, 0, 0, this.Job.runtime)).format("H:mm:ss");
		}

		private doLoadFaultDetail(defer: ng.IDeferred<IFaultDetail>, id: string) {
			var fault = _.find(this.faults, { iteration: id });
			if (_.isUndefined(fault)) {
				defer.reject();
			} else {
				this.$http.get(fault.faultUrl)
					.success((data: IFaultDetail) => { defer.resolve(data); })
					.error(reason => { defer.reject(reason); })
				;
			}
		}

		public LoadFaultDetail(id: string): ng.IPromise<IFaultDetail> {
			var defer = this.$q.defer<IFaultDetail>();
			if (this.pending) {
				this.pending.finally(() => { this.doLoadFaultDetail(defer, id); });
			} else {
				this.doLoadFaultDetail(defer, id);
			}
			return defer.promise;
		}

		public GetJobs(): ng.IPromise<IJob[]> {
			var params = { dryrun: false };
			var promise = this.$http.get(C.Api.Jobs, { params: params })
				.success((jobs: IJob[]) => this.jobs = jobs);
			return StripHttpPromise(this.$q, promise);
		}

		public Start(job: IJobRequest): ng.IPromise<IJob> {
			if (this.CanStart) {
				var promise = this.$http.post(C.Api.Jobs, job)
					.error(reason => {
						console.log('JobService.StartJob().error>', reason);
					})
				;
				return StripHttpPromise(this.$q, promise);
			}
		}

		public Delete(job: IJob): ng.IPromise<any> {
			return this.$http.delete(job.jobUrl)
				.then(() => { return this.GetJobs(); })
			;
		}
		
		public Continue() {
			this.sendCommand(
				this.CanContinue, 
				JobStatus.ContinuePending, 
				this.job.commands.continueUrl);
		}

		public Pause() {
			this.sendCommand(
				this.CanPause, 
				JobStatus.PausePending, 
				this.job.commands.pauseUrl);
		}

		public Stop() {
			this.sendCommand(
				this.CanStop, 
				JobStatus.StopPending, 
				this.job.commands.stopUrl);
		}
		
		public Kill() {
			this.sendCommand(
				this.CanStop,
				JobStatus.KillPending,
				this.job.commands.killUrl);
		}

		private sendCommand(check: boolean, status: string, url: string) {
			if (check) {
				this.job.status = status;
				this.$http.get(url)
					.success(() => this.startJobPoller())
					.error(reason => this.onError(reason));
			}
		}

		private onError(error: any) {
			console.log('onError', error);
			this.stopJobPoller();
		}

		private startJobPoller() {
			if (!_.isUndefined(this.poller)) {
				return;
			}
			
			this.poller = this.$interval(() => {
				this.$http.get(this.job.jobUrl)
					.success((job: IJob) => {
						var stopPending = (this.job.status === JobStatus.StopPending);
						var killPending = (this.job.status === JobStatus.KillPending);

						this.job = job;

						if (this.job.status !== JobStatus.Stopped) {
							if (stopPending) {
								this.job.status = JobStatus.StopPending;
							} else if (killPending) {
								this.job.status = JobStatus.KillPending;
							}
						}

						this.$rootScope['job'] = this.job;

						if (job.status === JobStatus.Stopped ||
							job.status === JobStatus.Paused) {
							this.stopJobPoller();
						}

						if (this.faults.length !== job.faultCount) {
							this.reloadFaults();
						}
					})
					.error(reason => this.onError(reason));
			}, JOB_INTERVAL);
		}
		
		private stopJobPoller() {
			this.$interval.cancel(this.poller);
			this.poller = undefined;
		}

		private reloadFaults(): ng.IHttpPromise<IFaultSummary[]> {
			return this.$http.get(this.job.faultsUrl)
				.success((faults: IFaultSummary[]) => {
					this.faults = faults;
				})
				.error(reason => this.onError(reason));
		}

		public LoadMetric<T>(metric: string): ng.IHttpPromise<T> {
			return this.$http.get(this.Job.metrics[metric]);
		}
	}
}
