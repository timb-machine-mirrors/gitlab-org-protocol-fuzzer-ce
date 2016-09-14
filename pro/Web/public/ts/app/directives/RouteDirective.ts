/// <reference path="../reference.ts" />

namespace Peach {
	export const RouteDirective: IDirective = {
		ComponentID: C.Directives.Route,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Route,
		controller: C.Controllers.Route,
		scope: {
			routes: '=',
			route: '=',
			index: '='
		}
	}

	export interface IRouteScope extends IFormScope {
		routes: IWebRoute[];
		route: IWebRoute;
		index: number;
		isOpen: boolean;
	}

	export class RouteController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IRouteScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			$scope.isOpen = true;
		}

		public get Header(): string {
			return this.$scope.route.url === '*' ? 'Default (*)' : this.$scope.route.url;
		}

		public get CanMoveUp(): boolean {
			return this.$scope.index !== 0;
		}

		public get CanMoveDown(): boolean {
			return this.$scope.index !== (this.$scope.routes.length - 1);
		}

		public OnMoveUp($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemUp(this.$scope.routes, this.$scope.index);
			this.$scope.form.$setDirty();
		}

		public OnMoveDown($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemDown(this.$scope.routes, this.$scope.index);
			this.$scope.form.$setDirty();
		}

		public OnRemove($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$scope.routes.splice(this.$scope.index, 1);
			this.$scope.form.$setDirty();
		}
	}
}
