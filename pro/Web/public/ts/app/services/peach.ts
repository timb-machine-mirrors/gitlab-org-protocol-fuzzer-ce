/// <reference path="../reference.ts" />

module DashApp.Services {
	"use strict";

	export interface IPeachService {
		URL_PREFIX: string;

		GetDefines(pitUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetFaultQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetDataQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetAutoQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetJobs(
			success: (data: Models.IJob) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		GetJobResource(jobUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetJobFaults(
			jobUrl: string,
			success: (job: Models.IFault[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		GetFault(
			faultUrl: string,
			success: (job: Models.IFault) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		GetLibraries(success: (data: Models.IPitLibrary[]) => void): void;

		GetPeachMonitors(
			success: (data: Models.IMonitor[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		GetPit(pitUrl: string, success: (data: Models.Pit) => void): void;

		CopyPit(request: Models.ICopyPitRequest,
			success: (data: Models.Pit) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		PostConfig(pitUrl: string, config: Models.IPitParameter[]): ng.IHttpPromise<any>;

		GetAgents(
			pitUrl: string,
			success: (data: Models.Agent[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		PostAgents(
			pitUrl: string,
			agents: Models.Agent[],
			success: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		PostAgentsPromise(
			pitUrl: string,
			agents: Models.Agent[]
		): ng.IHttpPromise<any>;

		TestConfiguration(
			pitUrl: string,
			success: (data: Models.IStartTestResponse) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		);

		StartJob(
			job: Models.IJob,
			success: (data: Models.IJob) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		PauseJob(
			jobUrl: string, success?: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		ContinueJob(
			jobUrl: string, success?: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		StopJob(
			jobUrl: string,
			success?: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;
		
		GetSingleResource(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetManyResources(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetState(
			success: (data: Models.IPair<string, string>[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void;

		GetFaultTimeline(
			jobUrl: string,
			success: (data: Models.IFaultTimelineMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>
		) => void);

		GetBucketTimeline(
			jobUrl: string,
			success: (data: Models.IBucketTimelineMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>
		) => void);

		GetBucketMetrics(
			jobUrl: string,
			success: (data: Models.IBucketMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>
		) => void);

		GetMutatorMetrics(
			jobUrl: string,
			success: (data: Models.IMutatorMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>
		) => void);

		GetElementMetrics(
			jobUrl: string,
			success: (data: Models.IElementMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>
		) => void);

		GetDatasetMetrics(
			jobUrl: string,
			success: (data: Models.IDatasetMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>
		) => void);

		GetStateMetrics(
			jobUrl: string,
			success: (data: Models.IStateMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>
		) => void);
	}

	export class PeachService implements IPeachService {
		private resource: ng.resource.IResourceService;
		private http: ng.IHttpService;

		public URL_PREFIX: string = "";

		constructor($resource: ng.resource.IResourceService, $http: ng.IHttpService) {
			this.resource = $resource;
			this.http = $http;
		}

		public GetFaultQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("testdata/wizard_qa_fault.json");
		}

		public GetDefines(pitUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource(this.URL_PREFIX + pitUrl + "/config");
		}

		public GetDataQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("testdata/wizard_qa_data.json");
		}

		public GetAutoQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("testdata/wizard_qa_auto.json");
		}

		public GetJobs(
			success: (data: Models.IJob) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			this.HttpGet<Models.IJob[]>("/p/jobs", (jobs: Models.IJob[]) => {
				if (jobs.length >= 0) {
					success(jobs[0]);
				}
				else {
					success(undefined);
				}
			}, error);
		}

		public GetJobResource(jobUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			if (this.isJobUrl(jobUrl)) {
				return this.resource(this.URL_PREFIX + jobUrl, {}, {
					get: {
						method: "GET",
						isArray: false,
						headers: {
							"Accept": "application/json",
							"Content-Type": "application/json"
						}
					}
				});
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetJobFaults(
			jobUrl: string,
			success: (job: Models.IFault[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IFault[]>(jobUrl + "/faults", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetFault(
			faultUrl: string,
			success: (job: Models.IFault) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			if (faultUrl != undefined && faultUrl.indexOf("/p/faults/") >= 0) {
				this.HttpGet<Models.IFault>(faultUrl, success, error);
			}
			else {
				throw "Not a Fault URL: " + faultUrl;
			}
		}

		public GetPit(
			pitUrl: string,
			success: (data: Models.Pit) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			if (this.isPitUrl(pitUrl)) {
				this.HttpGet(pitUrl, (pit: Models.Pit) => {
					var newpit: Models.Pit = new Models.Pit(pit);
					success(newpit);
				}, error);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public CopyPit(
			request: Models.ICopyPitRequest,
			success: (data: Models.Pit) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			this.http.post(this.URL_PREFIX + "/p/pits", request).then((response) => success(<Models.Pit>response.data), (response) => error(<ng.IHttpPromiseCallbackArg<any>>response));
		}

		public PostConfig(pitUrl: string, config: Models.IPitParameter[]): ng.IHttpPromise<any> {
			if (this.isPitUrl(pitUrl)) {

				var request: Models.IPostConfigRequest = {
					pitUrl: pitUrl,
					config: config
				};

				// remove me when linux can parse numbers
				for (var i = 0; i < request.config.length; i++) {
					request.config[i].max = undefined;
					request.config[i].min = undefined;
				}

				return this.http.post(this.URL_PREFIX + "/p/conf/wizard/config", request);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public GetPeachMonitors(
			success: (data: Models.IMonitor[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			this.HttpGet("/p/conf/wizard/monitors", success, error);
		}

		public GetAgents(
			pitUrl: string,
			success: (data: Models.Agent[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			if (this.isPitUrl(pitUrl)) {
				this.HttpGet(pitUrl + "/agents", success, error);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public PostAgents(
			pitUrl: string,
			agents: Models.Agent[],
			success: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			if (this.isPitUrl(pitUrl)) {
				var request: Models.IPostMonitorsRequest = {
					pitUrl: pitUrl,
					monitors: agents
				};

				// remove me when linux can parse numbers
				for (var a = 0; a < request.monitors.length; a++) {
					for (var m = 0; m < request.monitors[a].monitors.length; m++) {
						request.monitors[a].monitors[m].path = [];
					}
				}
				this.HttpPost("/p/conf/wizard/monitors", request, success, error);
				//return this.http.post(this.URL_PREFIX + "/p/conf/wizard/monitors", request);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public PostAgentsPromise(pitUrl: string, agents: Models.Agent[]): ng.IHttpPromise<any> {
			if (this.isPitUrl(pitUrl)) {
				var request: Models.IPostMonitorsRequest = {
					pitUrl: pitUrl,
					monitors: agents
				};

				// remove me when linux can parse numbers
				for (var a = 0; a < request.monitors.length; a++) {
					for (var m = 0; m < request.monitors[a].monitors.length; m++) {
						request.monitors[a].monitors[m].path = [];
					}
				}
				return this.http.post(this.URL_PREFIX + "/p/conf/wizard/monitors", request);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public TestConfiguration(
			pitUrl: string,
			success: (data: Models.IStartTestResponse) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isPitUrl(pitUrl)) {
				this.HttpGet<Models.IStartTestResponse>("/p/conf/wizard/test/start?pitUrl=" + pitUrl, success, error);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public GetLibraries(success: (data: Models.IPitLibrary[]) => void): void {
			this.HttpGet<Models.IPitLibrary[]>("/p/libraries", success, this.handleError);
		}

		public StartJob(
			job: Models.IJob,
			success: (data: Models.IJob) => void,
			error?: (response) => void
		) {
			if (this.isPitUrl(job.pitUrl)) {
				this.HttpPost<Models.IJob>("/p/jobs", job, success, error);
			}
			else {
				throw "Not a Pit URL: " + job.pitUrl;
			}
		}

		public PauseJob(
			jobUrl: string,
			success?: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<any>(jobUrl + "/pause", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public ContinueJob(
			jobUrl: string,
			success?: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<any>(jobUrl + "/continue", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public StopJob(
			jobUrl: string,
			success?: () => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<any>(jobUrl + "/stop", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetState(
			success: (data: Models.IPair<string, string>[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			this.HttpGet<Models.IPair<string, string>[]>("/p/conf/wizard/state", success, error);
		}

		public GetFaultTimeline(
			jobUrl: string,
			success: (data: Models.IFaultTimelineMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IFaultTimelineMetric[]>(jobUrl + "/metrics/faultTimeline", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetBucketTimeline(
			jobUrl: string,
			success: (data: Models.IBucketTimelineMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IBucketTimelineMetric[]>(jobUrl + "/metrics/bucketTimeline", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetBucketMetrics(
			jobUrl: string,
			success: (data: Models.IBucketMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IBucketMetric[]>(jobUrl + "/metrics/buckets", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetMutatorMetrics(
			jobUrl: string,
			success: (data: Models.IMutatorMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IMutatorMetric[]>(jobUrl + "/metrics/mutators", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetElementMetrics(
			jobUrl: string,
			success: (data: Models.IElementMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IElementMetric[]>(jobUrl + "/metrics/elements", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetDatasetMetrics(
			jobUrl: string,
			success: (data: Models.IDatasetMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IDatasetMetric[]>(jobUrl + "/metrics/dataset", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetStateMetrics(
			jobUrl: string,
			success: (data: Models.IStateMetric[]) => void,
			error?: (response: ng.IHttpPromiseCallbackArg<any>) => void
		) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<Models.IStateMetric[]>(jobUrl + "/metrics/states", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetSingleResource(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			if (url != undefined && url.indexOf("/p/") >= 0) {
				return this.resource(this.URL_PREFIX + url, {}, {
					get: {
						method: "GET",
						isArray: false,
						headers: {
							"Accept": "application/json",
							"Content-Type": "application/json"
						}
					}
				});
			}
			else {
				throw "Bad URL: " + url;
			}
		}

		public GetManyResources(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			if (url != undefined && url.indexOf("/p/") >= 0) {
				return this.resource(this.URL_PREFIX + url, {}, {
					get: {
						method: "GET",
						isArray: true,
						headers: {
							"Accept": "application/json",
							"Content-Type": "application/json"
						}
					}
				});
			}
			else {
				throw "Bad URL: " + url;
			}
		}

		private HttpGet<T>(
			url: string,
			success: (data?: T) => void,
			error: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			if (url != undefined && url.indexOf("/p/") >= 0) {
				if (error === undefined) {
					error = this.handleError;
				}
				this.http.get(this.URL_PREFIX + url).then((response) => {
					if (success !== undefined) {
						success(<T>response.data);
					}
				}, (response) => error(response));
			}
			else {
				throw "Bad URL: " + url;
			}
		}

		private HttpPost<T>(
			url: string,
			request: any,
			success: (data: T) => void,
			error: (response: ng.IHttpPromiseCallbackArg<any>) => void
		): void {
			if (url != undefined && url.indexOf("/p/") >= 0) {
				if (error === undefined) {
					error = this.handleError;
				}
				this.http.post(this.URL_PREFIX + url, request, null).then(
					(response) => success(<T>response.data),
					(response) => error(response)
				);
			}
			else {
				throw "Bad URL: " + url;
			}
		}

		private handleError(error) {
			console.error(error);
		}

		private isJobUrl(jobUrl: string): boolean {
			return (jobUrl != undefined && jobUrl.indexOf("/p/jobs/") >= 0);
		}

		private isPitUrl(pitUrl: string): boolean {
			return (pitUrl != undefined && pitUrl.indexOf("/p/pits/") >= 0);
		}
	}
} 
