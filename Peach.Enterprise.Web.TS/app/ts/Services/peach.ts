/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular-resource.d.ts" />

module DashApp.Services {
	"use strict";

	import P = DashApp.Models.Peach;
	import W = DashApp.Models.Wizard;

	export interface IPeachService {

		URL_PREFIX: string;

		GetSingleThing(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetManyThings(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetDefines(pitUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetFaultQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetDataQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetAutoQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetJobs(success: (data: P.Job[]) => void): void;

		GetLibraries(success: (data: P.PitLibrary[]) => void): void;

		GetPit(id: number, success: (data: P.Pit) => void): void;
		GetPit(url: string, success: (data: P.Pit) => void): void;
		CopyPit(request: P.CopyPitRequest, success: (data: P.Pit) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;

		PostConfig(pitUrl: string, config: P.PitConfigItem[]): ng.IHttpPromise<any>;
		PostMonitors(pitUrl: string, agents: W.Agent[]): ng.IHttpPromise<any>;
		TestConfiguration(pitUrl: string, success: (data: P.StartTestResponse) => void);

		GetLocalFile<T>(url: string, success: (data: T) => void, error?: (response) => void): void;

		StartJob(pitUrl: string, success: (data: P.Job) => void, error?: (response) => void): void;
		PauseJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		ContinueJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		StopJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
		//KillJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void;
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
		
		public GetJobs(success: (data: P.Job[]) => void): void {
			this.http.get(this.URL_PREFIX + "/p/jobs").then((response) => success(<P.Job[]>response.data));
		}

		public GetSingleThing(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
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

		public GetManyThings(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
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

		public GetPit(IdOrUrl: any, success: (data: P.Pit) => void): void { 
			if (typeof IdOrUrl == "number") {
				this.http.get(this.URL_PREFIX + "/p/pits/" + parseInt(IdOrUrl, 10)).then((response) => success(<P.Pit>response.data));
			}
			else if (typeof IdOrUrl == "string") {
				this.http.get(this.URL_PREFIX + IdOrUrl).then((response) => success(<P.Pit>response.data));
			}
			else {
				throw new Error("GetPit: Argument 0 is of an incompatible type.");
			}
		}

		public CopyPit(request: P.CopyPitRequest, success: (data: P.Pit) => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void): void {
			this.http.post(this.URL_PREFIX + "/p/pits", request).then((response) => success(<P.Pit>response.data), (response) => error(<ng.IHttpPromiseCallbackArg<any>>response));
		}
		
		public PostConfig(pitUrl: string, config: P.PitConfigItem[]): ng.IHttpPromise<any> {
			var request: P.PostConfigRequest = {
				pitUrl: pitUrl,
				config: config
			};

			return this.http.post(this.URL_PREFIX + "/p/conf/wizard/config", request);
		}

		public PostMonitors(pitUrl: string, agents: W.Agent[]): ng.IHttpPromise<any> {
			var request: P.PostMonitorsRequest = {
				pitUrl: pitUrl,
				monitors: agents
			};

			for (var a = 0; a < request.monitors.length; a++) {
				for (var m = 0; m < request.monitors[a].monitors.length; m++) {
					request.monitors[a].monitors[m].path = [];
				}
			}

			return this.http.post(this.URL_PREFIX + "/p/conf/wizard/monitors", request);
		}

		public TestConfiguration(pitUrl: string, success: (data: P.StartTestResponse) => void) {
			return this.http.get(this.URL_PREFIX + "/p/conf/wizard/test/start?pitUrl=" + pitUrl).then((response) => success(<P.StartTestResponse>response.data), (error) => {
				console.error(error.data); 
			});
		}

		public GetLibraries(success: (data: P.PitLibrary[]) => void): void {
			this.http.get(this.URL_PREFIX + "/p/libraries").then((response) => success(<P.PitLibrary[]>response.data), (error) => this.handleError(error));
		}

		public GetLocalFile<T>(url: string, success: (data: T) => void, error?: (response) => void): void {
			this.http.get(url).then((response) => success(<T>response.data), (response) => error(response));
		}

		public StartJob(pitUrl: string, success: (data: P.Job) => void, error?: (response) => void) {
			var job: P.Job = {
				pitUrl: pitUrl
			};

			if (error === undefined) {
				error = this.handleError;
			}

			this.http.post(this.URL_PREFIX + "/p/jobs", job).then((response) => {
				job.jobUrl = (<P.Job>response.data).jobUrl;
				success(job);
			}, (response) => error(response));
		}

		public PauseJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			if (error === undefined) {
				error = this.handleError;
			}

			this.http.get(this.URL_PREFIX + jobUrl + "/pause").then(() => success, (response) => error(response));
		}

		public ContinueJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			if (error === undefined) {
				error = this.handleError;
			}
			this.http.get(this.URL_PREFIX + jobUrl + "/continue").then(() => success, (response) => error(response));
		}

		public StopJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
			if (error === undefined) {
				error = this.handleError;
			}
			this.http.get(this.URL_PREFIX + jobUrl + "/stop").then(() => success, (response) => error(response));
		}

		//public KillJob(jobUrl: string, success?: () => void, error?: (response: ng.IHttpPromiseCallbackArg<any>) => void) {
		//	if (error === undefined) {
		//		error = this.handleError;
		//	}
		//	this.http.get(this.URL_PREFIX + jobUrl + "/kill").then(() => success, (response) => error(response));
		//}

		private handleError(error) {
			console.error(error);
		}

	}
} 