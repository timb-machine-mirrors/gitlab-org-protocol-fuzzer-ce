﻿/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var ParameterDirective: IDirective = {
		ComponentID: Constants.Directives.Parameter,
		restrict: 'E',
		templateUrl: 'html/directives/parameter.html',
		controller: Constants.Controllers.Parameter,
		scope: { param: '=' }
	}

	export var ParameterInputDirective: IDirective = {
		ComponentID: Constants.Directives.ParameterInput,
		restrict: 'E',
		templateUrl: 'html/directives/parameter-input.html',
		controller: Constants.Controllers.Parameter,
		scope: {
			param: '=',
			form: '='
		}
	}

	export interface IParameterScope extends IFormScope {		
		param: IParameter;
	}

	export interface IOption {
		key: string;
		text: string;
		description: string;
		group: string;
	}

	export class ParameterController {
		static $inject = [
			Constants.Angular.$scope,
			Constants.Services.Pit
		];

		constructor(
			private $scope: IParameterScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			this.makeChoices();
		}

		public get IsRequired(): boolean {
			return _.isUndefined(this.$scope.param.defaultValue);
		}

		public get IsReadonly() {
			return this.$scope.param.type === 'system';
		}

		public get ParamTooltip() {
			return this.IsReadonly ? this.$scope.param.value : '';
		}

		public get WidgetType(): string {
			switch (this.$scope.param.type) {
				case 'enum':
				case 'bool':
				case 'call':
					return 'select';
				default:
					return 'string';
			}
		}

		public Choices: IOption[];

		private makeChoices() {
			var options: string[];
			var group: string;
			if (this.$scope.param.type === 'call') {
				options = this.pitService.PitCalls || [];
				options = [''].concat(options);
				group = 'Calls';
			} else {
				options = this.$scope.param.options || [];
				group = 'Choices';
			}
			var tuples = [];

			options.forEach(item => {
				var option: IOption = {
					key: item,
					text: item || '<i>Undefined</i>',
					description: '',
					group: group
				};
				if (item === this.$scope.param.defaultValue) {
					option.group = 'Default';
					tuples.unshift(option);
				} else {
					tuples.push(option);
				}
			});
			this.Choices = tuples.concat(this.defines());
		}

		private defines(): IOption[] {
			return _.map(this.pitService.PitConfig.config, param => {
				var key = '##' + param.key + '##'; 
				return <IOption> {
					key: key,
					text: key,
					description: param.description,
					group: 'Defines'
				};
			});
		}
	}
}
