/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IBoundScope extends ng.IScope {
		min: IBoundFunction;
		max: IBoundFunction;
	}

	export interface IBoundFunction {
		(): number;
	}

	interface IValidatePredicate {
		(value: any): boolean;
	}

	function predicateValidation(
		name: string,
		ctrl: ng.INgModelController,
		predicate: IValidatePredicate
	) {
		ctrl.$validators[name] = (modelValue, viewValue) => {
			var value = modelValue || viewValue;
			return _.isUndefined(value)
				|| (_.isString(value) && _.isEmpty(value))
				|| predicate(value);
		};
	}

	export var RangeDirective: IDirective = {
		ComponentID: Constants.Directives.Range,
		restrict: 'A',
		require: Constants.Angular.ngModel,
		scope: {
			min: '&peachRangeMin',
			max: '&peachRangeMax'
		},
		link: (
			scope: IBoundScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			predicateValidation('rangeMin', ctrl, (value: string) => {
				var int = parseInt(value);
				var min = scope.min();
				return _.isUndefined(min) || (!_.isNaN(int) && int >= min);
			});
			predicateValidation('rangeMax', ctrl, (value: string) => {
				var int = parseInt(value);
				var max = scope.max();
				return _.isUndefined(max) || (!_.isNaN(int) && int <= max);
			});
		}
	}

	export var IntegerDirective: ng.IDirective = {
		ComponentID: Constants.Directives.Integer,
		restrict: 'A',
		require: Constants.Angular.ngModel,
		link: (
			scope: ng.IScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			var pattern = /^(\-|\+)?\d+$/;
			predicateValidation(Constants.Directives.Integer, ctrl,
				(value: string) => pattern.test(value)
			);
		}
	}

	export var HexDirective: ng.IDirective = {
		ComponentID: Constants.Directives.HexString,
		restrict: 'A',
		require: Constants.Angular.ngModel,
		link: (
			scope: ng.IScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			var pattern = /^[0-9A-Fa-f]+$/;
			predicateValidation(Constants.Directives.HexString, ctrl,
				(value: string) => pattern.test(value)
			);
		}
	}
}
