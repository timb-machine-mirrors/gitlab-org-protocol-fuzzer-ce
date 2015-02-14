/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	interface IUnsavedScope extends ng.IScope {
		ctrl: UnsavedController;
	}

	export var UnsavedDirective: IDirective = {
		ComponentID: C.Directives.Unsaved,
		restrict: 'A',
		require: '^form',
		controller: C.Controllers.Unsaved,
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
			C.Angular.$scope,
			C.Angular.$modal,
			C.Angular.$state
		];	

		constructor(
			private $scope: ng.IScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private $state: ng.ui.IStateService
		) {
		}

		public Link(form: ng.IFormController) {
			var onRouteChangeOff = this.$scope.$root.$on(C.Angular.$stateChangeStart, (
				event: ng.IAngularEvent,
				toState: ng.ui.IState,
				toParams: any,
				fromState: ng.ui.IState,
				fromParams: any
			) => {
				if (!form.$dirty) {
					onRouteChangeOff();
					return;
				}

				event.preventDefault();

				var modal = this.$modal.open({
					templateUrl: C.Templates.Modal.Unsaved,
					controller: UnsavedModalController
				});
				modal.result.then((result) => {
					if (result === 'ok') {
						onRouteChangeOff();
						this.$state.transitionTo(toState.name, toParams);
					}
				});
			});
		}
	}

	class UnsavedModalController {

		static $inject = [
			C.Angular.$scope,
			C.Angular.$modalInstance
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
