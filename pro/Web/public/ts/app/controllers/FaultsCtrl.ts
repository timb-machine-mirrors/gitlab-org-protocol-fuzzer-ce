/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class FaultsController {

		static $inject = [
			"$scope",
			"$routeParams",
			"JobService",
			"FaultDetailResource"
		];

		constructor(
			$scope: IViewModelScope,
			$routeParams: ng.route.IRouteParamsService,
			private jobService: Services.JobService,
			private faultDetailResource: Models.IFaultDetailResource
		) {
			$scope.vm = this;
			this.bucket = $routeParams['bucket'];
			if (this.bucket === "all") {
				this.Title = "All Faults";
			} else {
				this.Title = "Faults For " + this.bucket;
				this.refreshBucketFaults();

				$scope.$watch('vm.jobService.Faults.length', (newVal, oldVal) => {
					if (newVal !== oldVal) {
						this.refreshBucketFaults();
					}
				});
			}

			this.Faults = _.clone(this.AllFaults);
		}

		public get Job(): Models.IJob {
			return this.jobService.Job;
		}

		public get AllFaults(): Models.IFaultSummary[] {
			if (this.bucket === "all") {
				return this.jobService.Faults;
			}
			return this.bucketFaults;
		}

		private bucket: string;
		private bucketFaults: Models.IFaultSummary[];

		public Faults: Models.IFaultSummary[];

		public get IsFaultSelected(): boolean {
			return !_.isUndefined(this.CurrentFault);
		}

		public Title: string = "All Faults";

		public CurrentFault: Models.IFaultDetail;

		public IsDetailActive: boolean;

		public OnFaultSelected(fault: Models.IFaultSummary) {
			var promise = this.faultDetailResource.get({ id: ExtractId('faults', fault.faultUrl) });
			promise.$promise.then((detail: Models.IFaultDetail) => {
				this.CurrentFault = detail;
				this.IsDetailActive = true;
			});
		}

		private refreshBucketFaults() {
			this.bucketFaults = _.filter(this.jobService.Faults, (fault: Models.IFaultSummary) => {
				return this.bucket === (fault.majorHash + '_' + fault.minorHash);
			});
		}
	}
}
