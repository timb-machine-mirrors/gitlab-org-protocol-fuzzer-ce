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
		var validator = value => {
			var isValid = (_.isEmpty(value) || predicate(value));
			ctrl.$setValidity(name, isValid);
			return value;
		};

		ctrl.$parsers.push(validator);
		ctrl.$formatters.push(validator);
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
			if (scope.min) {
				predicateValidation('rangeMin', ctrl,
					(value: number) => (value >= scope.min())
				);
			}
			if (scope.max) {
				predicateValidation('rangeMax', ctrl,
					(value: number) => (value <= scope.max())
				);
			}
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
			var pattern = /^\-?\d+$/;
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
