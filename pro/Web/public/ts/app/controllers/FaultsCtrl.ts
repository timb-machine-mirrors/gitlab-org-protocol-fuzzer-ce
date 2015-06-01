/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IFaultDetailScope extends IFaultSummaryScope {
		FaultDetailTitle: string;
	}

	function FaultsTitle(bucket: string) {
		return (bucket === "all") ? 'Faults' : 'Bucket: ' + bucket;
	}

	export class FaultsDetailController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IFaultDetailScope,
			$state: ng.ui.IStateService,
			jobService: JobService
		) {
			$scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);

			var id = $state.params['id'];
			$scope.FaultDetailTitle = 'Iteration: ' + id;
			var promise = jobService.LoadFaultDetail(id);
			promise.then((detail: IFaultDetail) => {
				this.Fault = detail;
			}, () => {
				$state.go(C.States.MainHome);
			});
		}

		public Fault: IFaultDetail;
	}

	export interface IFaultSummaryScope extends IViewModelScope {
		FaultSummaryTitle: string;
	}

	export class FaultsController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IFaultSummaryScope,
			private $state: ng.ui.IStateService,
			private jobService: JobService
		) {
			this.bucket = $state.params['bucket'];
			$scope.FaultSummaryTitle = FaultsTitle(this.bucket);
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
