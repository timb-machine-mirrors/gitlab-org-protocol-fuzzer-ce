/// <reference path="../reference.ts" />

namespace Peach {
	export class AddMonitorController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			C.Services.Pit
		];

		private selected: IParameter;
		public Monitors: IParameter[];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService
		) {
			$scope.vm = this;
			this.Monitors = pitService.Pit.metadata.monitors;
		}

		public get CanAccept(): boolean {
			return !_.isUndefined(this.selected);
		}

		public OnSelect(item: IParameter): void {
			this.selected = item;
		}
		
		public Accept(): void {
			this.$modalInstance.close(this.selected);
		}

		public Cancel(): void {
			this.$modalInstance.dismiss();
		}
	}
}
