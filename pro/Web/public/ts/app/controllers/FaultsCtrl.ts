/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class FaultsController {

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$routeParams,
			Constants.Services.Job,
			"FaultDetailResource"
		];

		constructor(
			$scope: IViewModelScope,
			$routeParams: ng.route.IRouteParamsService,
			private jobService: JobService,
			private faultDetailResource: IFaultDetailResource
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

		public get Job(): IJob {
			return this.jobService.Job;
		}

		public get AllFaults(): IFaultSummary[] {
			if (this.bucket === "all") {
				return this.jobService.Faults;
			}
			return this.bucketFaults;
		}

		private bucket: string;
		private bucketFaults: IFaultSummary[];

		public Faults: IFaultSummary[];

		public get IsFaultSelected(): boolean {
			return !_.isUndefined(this.CurrentFault);
		}

		public Title: string = "All Faults";

		public CurrentFault: IFaultDetail;

		public IsDetailActive: boolean;

		public OnFaultSelected(fault: IFaultSummary) {
			var promise = this.faultDetailResource.get({ id: ExtractId('faults', fault.faultUrl) });
			promise.$promise.then((detail: IFaultDetail) => {
				this.CurrentFault = detail;
				this.IsDetailActive = true;
			});
		}

		private refreshBucketFaults() {
			this.bucketFaults = _.filter(this.jobService.Faults, (fault: IFaultSummary) => {
				return this.bucket === (fault.majorHash + '_' + fault.minorHash);
			});
		}
	}
}
