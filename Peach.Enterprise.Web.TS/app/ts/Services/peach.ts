
module DashApp.Services {
	"use strict";

	import P = DashApp.Models.Peach;
	import W = DashApp.Models.Wizard;

	export interface IPeachService {

		URL_PREFIX: string;

		//GetSingleThing(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		//GetManyThings(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetDefines(pitUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetFaultQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetDataQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetAutoQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetJob(success: (data: P.Job) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		GetJobResource(jobUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetJobFaults(jobUrl: string, success: (job: P.Fault[]) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		GetFault(faultUrl: string, success: (job: P.Fault) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;

		GetLibraries(success: (data: P.PitLibrary[]) => void): void;

		GetPit(pitUrl: string, success: (data: P.Pit) => void): void;
		CopyPit(request: P.CopyPitRequest, success: (data: P.Pit) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;

		PostConfig(pitUrl: string, config: P.PitConfigItem[]): ng.IHttpPromise<any>;
		PostMonitors(pitUrl: string, agents: W.Agent[]): ng.IHttpPromise<any>;
		TestConfiguration(pitUrl: string, success: (data: P.StartTestResponse) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void);

		StartJob(pitUrl: string, success: (data: P.Job) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		PauseJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		ContinueJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		StopJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		//KillJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;

		GetSingleResource(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetManyResources(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetState(success: (data: W.StateItem[]) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
	}


	export class PeachService implements IPeachService {
		private resource: ng.resource.IResourceService;
		private http: ng.IHttpService;

		//public URL_PREFIX: string = "http://localhost:8888"; 
		public URL_PREFIX: string = "";

		constructor($resource: ng.resource.IResourceService, $http: ng.IHttpService) {
			this.resource = $resource;
			this.http = $http;
		}

		public GetFaultQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("../testdata/wizard_qa_fault.json");
		}

		public GetDefines(pitUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource(this.URL_PREFIX + pitUrl + "/config");
		}

		public GetDataQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("../testdata/wizard_qa_data.json");
		}

		public GetAutoQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("../testdata/wizard_qa_auto.json");
		}
		
		public GetJob(success: (data: P.Job) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {
			this.HttpGet<P.Job[]>("/p/jobs",(jobs: P.Job[]) => {
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

		public GetJobFaults(jobUrl: string, success: (job: P.Fault[]) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<P.Fault[]>(jobUrl + "/faults", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetFault(faultUrl: string, success: (job: P.Fault) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {
			if (faultUrl != undefined && faultUrl.indexOf("/p/faults/") >= 0) {
				this.HttpGet<P.Fault>(faultUrl, success, error);
			}
			else {
				throw "Not a Fault URL: " + faultUrl;
			}
		}

		public GetPit(pitUrl: string, success: (data: P.Pit) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {  
			if(this.isPitUrl(pitUrl)) {
				this.HttpGet(pitUrl, (pit: P.Pit) => {
					var newpit: P.Pit = new P.Pit(pit);
					success(newpit);
				}, error);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public CopyPit(request: P.CopyPitRequest, success: (data: P.Pit) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {
			this.http.post(this.URL_PREFIX + "/p/pits", request).then((response) => success(<P.Pit>response.data), (response) => error(<ng.IHttpPromiseCallbackArg<any>>response));
		}
		
		public PostConfig(pitUrl: string, config: P.PitConfigItem[]): ng.IHttpPromise<any> {
			if (this.isPitUrl(pitUrl)) {

				var request: P.PostConfigRequest = {
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

		public PostMonitors(pitUrl: string, agents: W.Agent[]): ng.IHttpPromise<any> {
			if (this.isPitUrl(pitUrl)) {
				var request: P.PostMonitorsRequest = {
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

		public TestConfiguration(pitUrl: string, success: (data: P.StartTestResponse) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			if (this.isPitUrl(pitUrl)) {
				this.HttpGet<P.StartTestResponse>("/p/conf/wizard/test/start?pitUrl=" + pitUrl, success, error);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public GetLibraries(success: (data: P.PitLibrary[]) => void): void {
			this.HttpGet<P.PitLibrary[]>("/p/libraries", success, this.handleError);
		}

		public StartJob(pitUrl: string, success: (data: P.Job) => void, error?: (response) => void) {
			if (this.isPitUrl(pitUrl)) {
				var job: P.Job = {
					pitUrl: pitUrl
				}
				this.HttpPost<P.Job>("/p/jobs", job, success, error);
			}
			else {
				throw "Not a Pit URL: " + pitUrl;
			}
		}

		public PauseJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			if(this.isJobUrl(jobUrl)) {
				this.HttpGet<any>(jobUrl + "/pause", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public ContinueJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<any>(jobUrl + "/continue", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public StopJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			if (this.isJobUrl(jobUrl)) {
				this.HttpGet<any>(jobUrl + "/stop", success, error);
			}
			else {
				throw "Not a Job URL: " + jobUrl;
			}
		}

		public GetState(success: (data: W.StateItem[]) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			this.HttpGet<any>("/p/conf/wizard/state", success, error);
		}


		//public KillJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
		//	if (error === undefined) {
		//		error = this.handleError;
		//	}
		//	this.http.get(this.URL_PREFIX + jobUrl + "/kill").then(() => success, (response) => error(response));
		//}

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


		private HttpGet<T>(url: string, success: (data?: T) => void, error: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {
			if (url != undefined && url.indexOf("/p/") >= 0) {
				if (error === undefined) {
					error = this.handleError;
				}
				this.http.get(this.URL_PREFIX + url).then((response) => {
					try {
						success(<T>response.data);
					} catch (error) {
						success();
					}
				}, (response) => error(response));
			}
			else {
				throw "Bad URL: " + url;
			}
		}

		private HttpPost<T>(url: string, request: any, success: (data: T) => void, error: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {
			if (url != undefined && url.indexOf("/p/") >= 0) {
				if (error === undefined) {
					error = this.handleError;
				}
				this.http.post(this.URL_PREFIX + url, request, null).then((response) => success(<T>response.data), (response) => error(response));
			}
			else {
				throw "Bad URL: " + url;
			}
		}

		private handleError(error) {
			console.error(error);
		}

		private isJobUrl(jobUrl: string): boolean {
			return (jobUrl != undefined && jobUrl.indexOf("/p/jobs/") >= 0)
		}

		private isPitUrl(pitUrl: string): boolean {
			return (pitUrl != undefined && pitUrl.indexOf("/p/pits/") >= 0)
		}
	}
} 