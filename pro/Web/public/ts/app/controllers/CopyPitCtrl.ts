/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class CopyPitController {
		public Error: string = "";

		static $inject = [
			C.Angular.$scope,
			C.Angular.$modalInstance,
			C.Services.Pit,
			"Pit"
		];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService,
			public Pit: IPit
		) {
			$scope.vm = this;
		}

		public Submit() {
			this.Error = "";

//			var promise = this.pitService.SaveTemplate(this.Pit);
//			promise.then((response: ng.IHttpPromiseCallbackArg<IPit>) => {
//				this.$modalInstance.close(response.data);
//			}, (response: ng.IHttpPromiseCallbackArg<IPit>) => {
//				switch (response.status) {
//				case 400:
//					this.Error = this.Pit.name + " already exists, please choose a new name.";
//					break;
//				default:
//					this.Error = "Error: " + response.statusText;
//					break;
//				}
//			});
		}

		public Cancel() {
			this.$modalInstance.dismiss();
		}
	}
}
