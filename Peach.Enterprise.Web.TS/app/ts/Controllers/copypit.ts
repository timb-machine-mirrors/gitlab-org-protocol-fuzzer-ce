module DashApp {
	import P = DashApp.Models.Peach;

	export class CopyPitController {
		private modalInstance: ng.ui.bootstrap.IModalServiceInstance;
		public pit: P.Pit;
		
		constructor($scope: ViewModelScope, $modalInstance: ng.ui.bootstrap.IModalServiceInstance, pit: P.Pit) {
			$scope.vm = this;
			this.modalInstance = $modalInstance;
			this.pit = pit;
		}

		submit() {
			this.modalInstance.close(this.pit);
		}
	}
}