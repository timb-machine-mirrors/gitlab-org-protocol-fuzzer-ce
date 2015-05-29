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
		
		public OnEnter(id) {
			this.$http.get(C.Api.JobUrl.replace(':id', id))
				.success((job: IJob) => {
					this.job = job;
					this.$rootScope['job'] = job;
					if (this.job.faultCount > 0) {
						this.ReloadFaults();
					}
					if (this.job.status !== JobStatus.Stopped) {
						this.StartJobPoller();
					}
				});
		}
		
		public OnExit() {
			this.StopJobPoller();
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
			return this.CheckStatus([JobStatus.Paused]);
		}

		public get CanPause(): boolean {
			return this.CheckStatus([JobStatus.Running]);
		}

		public get CanStop(): boolean {
			return this.CheckStatus([
				JobStatus.Running,
				JobStatus.Paused,
				JobStatus.StartPending,
				JobStatus.PausePending,
				JobStatus.ContinuePending
			]);
		}

		private CheckStatus(good: string[]): boolean {
			return this.Job && _.contains(good, this.Job.status);
		}

		public get RunningTime(): string {
			if (_.isUndefined(this.Job)) {
				return undefined;
			}
			return moment(new Date(0, 0, 0, 0, 0, this.Job.runtime)).format("H:mm:ss");
		}

		public LoadFaultDetail(id: string): ng.IPromise<IFaultDetail> {
			var fault = _.find(this.faults, { iteration: id });
			if (_.isUndefined(fault)) {
				var defer = this.$q.defer<IFaultDetail>();
				defer.reject();
				return defer.promise;
			}
			return StripHttpPromise(this.$q, this.$http.get(fault.faultUrl));
		}

		public GetJobs(): ng.IPromise<IJob[]> {
			var params = { filter: 'dryRun' };
			var promise = this.$http.get(C.Api.Jobs, { params: params })
				.success((jobs: IJob[]) => this.jobs = jobs);
			return StripHttpPromise(this.$q, promise);
		}

		public Start(job: IJobRequest): ng.IPromise<IJob> {
			if (this.CanStart) {
				var promise = this.$http.post(C.Api.Jobs, job);
				promise.error(reason => {
					console.log('JobService.StartJob().error>', reason);
				});
				return StripHttpPromise(this.$q, promise);
			}
		}

		public Delete(job: IJob): ng.IPromise<any> {
			var promise = this.$http.delete(job.jobUrl);
			promise.success(() => {
				return this.GetJobs();
			});
			return StripHttpPromise(this.$q, promise);
		}
		
		public Continue() {
			this.SendCommand(
				this.CanContinue, 
				JobStatus.ContinuePending, 
				this.job.commands.continueUrl);
		}

		public Pause() {
			this.SendCommand(
				this.CanPause, 
				JobStatus.PausePending, 
				this.job.commands.pauseUrl);
		}

		public Stop() {
			this.SendCommand(
				this.CanStop, 
				JobStatus.StopPending, 
				this.job.commands.stopUrl);
		}
		
		private SendCommand(check: boolean, status: string, url: string) {
			if (check) {
				this.job.status = status;
				this.$http.get(url)
					.success(() => this.StartJobPoller())
					.error(reason => this.OnError(reason));
			}
		}

		private OnError(error: any) {
			console.log('onError', error);
			this.StopJobPoller();
		}

		private StartJobPoller() {
			if (!_.isUndefined(this.poller)) {
				return;
			}
			
			this.poller = this.$interval(() => {
				var promise = this.$http.get(this.job.jobUrl);
				promise.success((job: IJob) => {
					this.job = job;
					this.$rootScope['job'] = job;
					if (job.status === JobStatus.Stopped ||
						job.status === JobStatus.Paused) {
						this.StopJobPoller();
					}
					if (this.faults.length !== job.faultCount) {
						this.ReloadFaults();
					}
				});
				promise.error(reason => this.OnError(reason));
			}, JOB_INTERVAL);
		}
		
		private StopJobPoller() {
			this.$interval.cancel(this.poller);
			this.poller = undefined;
		}

		private ReloadFaults() {
			this.$http.get(this.job.faultsUrl)
				.success((faults: IFaultSummary[]) => {
					this.faults = faults;
				})
				.error(reason => this.OnError(reason));
		}
	}
}
