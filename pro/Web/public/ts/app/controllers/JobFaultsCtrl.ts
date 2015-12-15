/// <reference path="../reference.ts" />

namespace Peach {
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

			const id = $state.params['id'];
			$scope.FaultDetailTitle = 'Iteration: ' + id;
			const promise = jobService.LoadFaultDetail(id);
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
			C.Angular.$state
		];

		constructor(
			$scope: IFaultSummaryScope,
			$state: ng.ui.IStateService
		) {
			$scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);
		}
	}
}
