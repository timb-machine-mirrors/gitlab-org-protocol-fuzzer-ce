/// <reference path="../reference.ts" />

namespace Peach {
	"use strict";

	export interface IRatioScope extends ng.IScope {
		stRatio: number;
	}

	export var SmartTableRatioDirective: IDirective = {
		ComponentID: C.Directives.Ratio,
		restrict: 'A',
		scope: {
			stRatio: '='
		},
		link: (scope: IRatioScope, element: ng.IAugmentedJQuery) => {
			element.css('width', scope.stRatio + '%');
		}
	}
}
