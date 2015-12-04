/// <reference path="../reference.ts" />

namespace Peach {
	export var DefinesDirective: IDirective = {
		ComponentID: C.Directives.Defines,
		restrict: "E",
		templateUrl: C.Templates.Directives.Defines,
		controller: C.Controllers.Defines,
		scope: { group: "=" }
	}

	export interface IDefinesScope extends IFormScope {
		group: IParameter;
		isOpen: boolean;
	}

	export class DefinesController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IDefinesScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			if (!$scope.group.collapsed) {
				$scope.isOpen = true;
			}
		}
	}
}
