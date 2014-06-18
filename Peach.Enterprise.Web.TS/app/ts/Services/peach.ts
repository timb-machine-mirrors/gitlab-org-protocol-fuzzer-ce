﻿/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular-resource.d.ts" />

module DashApp.Services {
	import P = DashApp.Models.Peach;

	export interface IPeachService {
		GetDefines(pitUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetFaultQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetDataQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetAutoQA(): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		GetJobs(success: (data: P.Job[]) => void): void;
		//GetJob(jobUrl: string);

		GetSingleThing(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetManyThings(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;

		//GetJobFaults(jobUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetPit(id: number, success: (data: P.Pit) => void): void;
		GetPit(url: string, success: (data: P.Pit) => void): void;
		CopyPit(request: P.CopyPitRequest, success: (data: P.Pit) => void): void;

		TestConfiguration(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		GetLibraries(success: (data: P.PitLibrary[]) => void): void;

		//PostPitConfiguration(): ng.resource.IResourceClass<ng.resource.IResource<any>>;
		//PostMonitorConfiguration(): ng.resource.IResourceClass<ng.resource.IResource<any>>;

	}


	export class PeachService implements IPeachService {
		private resource: ng.resource.IResourceService;
		private http: ng.IHttpService;

		constructor($resource: ng.resource.IResourceService, $http: ng.IHttpService) {
			this.resource = $resource;
			this.http = $http;
		}

		public GetFaultQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("../testdata/wizard_qa_fault.json");
		}

		public GetDefines(pitUrl: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource(pitUrl + "/config");
		}

		public GetDataQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("../testdata/wizard_qa_data.json");
		}

		public GetAutoQA(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("../testdata/wizard_qa_auto.json");
		}
		
		public GetJobs(success: (data: P.Job[]) => void): void {
			this.http.get("/p/jobs").success((data) => success(<P.Job[]>data));
		}

		public GetSingleThing(url: string): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource(url, {}, {
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
			return this.resource(url, {}, {
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
				this.http.get("/p/pits/" + parseInt(IdOrUrl)).success((data) => success(<P.Pit>data));
			}
			else if (typeof IdOrUrl == "string") {
				this.http.get(IdOrUrl).success((data) => success(<P.Pit>data));
			}
			else {
				throw new Error("GetPit: Argument 0 is of an incompatible type.");
			}
		}

		public CopyPit(request: P.CopyPitRequest, success: (data: P.Pit) => void): void {
			this.http.post("/p/pits", request).success((data) => success(<P.Pit>data));
		}
		
		public PostPitConfiguration(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("/p/conf/wizard/config");
		}

		public PostMonitorConfiguration(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("/p/conf/wizard/monitors");
		}

		public TestConfiguration(): ng.resource.IResourceClass<ng.resource.IResource<any>> {
			return this.resource("/testdata/test_results.json");
		}

		public GetLibraries(success: (data: P.PitLibrary[]) => void): void {
			this.http.get("/p/libraries").success((data) => {
				var libs: P.PitLibrary[] = <P.PitLibrary[]>data;
				success(libs);
			});
		}

		public OpenTestResults() {
			
			
		}
	}
} 