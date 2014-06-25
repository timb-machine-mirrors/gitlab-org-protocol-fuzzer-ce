module DashApp {
	"use strict";

	import P = DashApp.Models.Peach;

	export class CopyPitController {
		private modalInstance: ng.ui.bootstrap.IModalServiceInstance;
		private libraryUrl: string;
		private peachSvc: Services.IPeachService;


		public pit: P.Pit;
		public error: string = "";
		
		constructor($scope: ViewModelScope, $modalInstance: ng.ui.bootstrap.IModalServiceInstance, pit: P.Pit, libraryUrl: string, peachSvc: Services.IPeachService) {
			$scope.vm = this;
			this.modalInstance = $modalInstance;
			this.pit = pit;
			this.libraryUrl = libraryUrl;
			this.peachSvc = peachSvc;
		}

		submit() {
			this.error = "";

			var request: P.CopyPitRequest = {
				libraryUrl: this.libraryUrl,
				pit: this.pit
			};

			this.peachSvc.CopyPit(request, (data: P.Pit) => {
				this.modalInstance.close(data);
			}, (response: ng.IHttpPromiseCallbackArg<any>) => {
				console.error(response);
				switch (response.status) {
					case 400:
						this.error = this.pit.name + " already exists, please choose a new name.";
						break;
					default:
						this.error = "Error: " + response.statusText;
				}
			});


		}
	}
}