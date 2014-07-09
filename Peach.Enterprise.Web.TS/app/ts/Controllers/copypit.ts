/// <reference path="../../../scripts/typings/angular-ui-bootstrap/angular-ui-bootstrap.d.ts" />
/// <reference path="../services/peach.ts" />


module DashApp {
	"use strict";

	

	export class CopyPitController {
		private modalInstance: ng.ui.bootstrap.IModalServiceInstance;
		private libraryUrl: string;
		private peachSvc: Services.IPeachService;


		public pit: Models.Pit;
		public error: string = "";
		
		constructor($scope: ViewModelScope, $modalInstance: ng.ui.bootstrap.IModalServiceInstance, pit: Models.Pit, libraryUrl: string, peachSvc: Services.IPeachService) {
			$scope.vm = this;
			this.modalInstance = $modalInstance;
			this.pit = pit;
			this.libraryUrl = libraryUrl;
			this.peachSvc = peachSvc;
		}

		submit() {
			this.error = "";

			var request: Models.CopyPitRequest = {
				libraryUrl: this.libraryUrl,
				pit: this.pit
			};

			this.peachSvc.CopyPit(request, (data: Models.Pit) => {
				this.modalInstance.close(data);
			}, (response: ng.IHttpPromiseCallbackArg<any>) => {
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