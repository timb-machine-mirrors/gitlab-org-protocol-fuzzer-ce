/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	function FaultsTitleParts($stateParams) {
		var parts = [];
		var bucket = $stateParams.bucket;
		if (bucket === "all") {
			parts.push('All');
		} else {
			parts.push('Bucket: ' + bucket);
		}

		if ($stateParams.id) {
			parts.push('Iteration: ' + $stateParams.id);
		}
		return parts;
	}

	export class FaultsDetailController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			$state: ng.ui.IStateService,
			jobService: JobService
		) {
			this.Title = FaultsTitleParts($state.params);
			var promise = jobService.LoadFaultDetail($state.params['id']);
			promise.then((detail: IFaultDetail) => {
				this.Fault = detail;
			}, () => {
				$state.go(C.States.MainHome);
			});
		}

		public Title: string[];
		public Fault: IFaultDetail;
	}

	export class FaultsController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private jobService: JobService
		) {
			this.bucket = $state.params['bucket'];
			this.Title = FaultsTitleParts($state.params)[0];
			if (this.bucket !== "all") {
				this.refreshBucketFaults();
				$scope.$watch(() => jobService.Faults.length, (newVal, oldVal) => {
					if (newVal !== oldVal) {
						this.refreshBucketFaults();
					}
				});
			}

			this.Faults = _.clone(this.AllFaults);
		}

		private bucket: string;
		private bucketFaults: IFaultSummary[];

		public Faults: IFaultSummary[];
		public Title: string;

		public get AllFaults(): IFaultSummary[] {
			if (this.bucket === "all") {
				return this.jobService.Faults;
			}
			return this.bucketFaults;
		}

		public OnFaultSelected(fault: IFaultSummary) {
			var params = {
				bucket: this.bucket,
				id: fault.iteration
			};
			this.$state.go(C.States.JobFaultsDetail, params);
		}

		private refreshBucketFaults() {
			this.bucketFaults = _.filter(this.jobService.Faults, (fault: IFaultSummary) => {
				return this.bucket === (fault.majorHash + '_' + fault.minorHash);
			});
		}
	}
}
