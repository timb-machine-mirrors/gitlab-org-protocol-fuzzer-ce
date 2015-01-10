/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	interface IUnsavedScope extends ng.IScope {
		ctrl: UnsavedController;
	}

	export var UnsavedDirective: IDirective = {
		ComponentID: Constants.Directives.Unsaved,
		restrict: 'A',
		require: '^form',
		controller: Constants.Controllers.Unsaved,
		controllerAs: 'ctrl',
		scope: {},
		link: (
			scope: IUnsavedScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			form: ng.IFormController
		) => {
			scope.ctrl.Link(form);
		}
	}

	export class UnsavedController {
		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$modal,
			Constants.Angular.$location
		];	

		constructor(
			private $scope: ng.IScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private $location: ng.ILocationService
		) {
		}

		public Link(form: ng.IFormController) {
			var onRouteChangeOff = this.$scope.$root.$on('$locationChangeStart', (
				event: ng.IAngularEvent,
				newUrl: string
			) => {

				if (!form.$dirty) {
					onRouteChangeOff();
					return;
				}

				var modal = this.$modal.open({
					templateUrl: "html/modal/Unsaved.html",
					controller: UnsavedModalController
				});
				modal.result.then((result) => {
					if (result === 'ok') {
						onRouteChangeOff();

						var path = newUrl.substr(newUrl.indexOf('#') + 1);
						this.$location.path(path);
					}
				});

				event.preventDefault();
			});
		}
	}

	class UnsavedModalController {

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$modalInstance
		];

		constructor(
			private $scope: IFormScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance
		) {
			$scope.vm = this;
		}

		public OnCancel() {
			this.$modalInstance.dismiss();
		}

		public OnSubmit() {
			this.$modalInstance.close('ok');
		}
	}
}
