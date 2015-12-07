/// <reference path="../reference.ts" />

namespace Peach {
	export var ParameterInputDirective: IDirective = {
		ComponentID: C.Directives.ParameterInput,
		restrict: "E",
		templateUrl: C.Templates.Directives.ParameterInput,
		controller: C.Controllers.ParameterInput,
		scope: {
			param: "=",
			form: "="
		}
	}

	export interface IOption {
		key: string;
		text: string;
		description: string;
		group: string;
	}

	export class ParameterInputController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IParameterScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			this.MakeChoices();
		}

		get IsRequired(): boolean {
			return _.isUndefined(this.$scope.param.optional) || !this.$scope.param.optional;
		}

		get IsReadonly() {
			return this.$scope.param.type === ParameterType.System;
		}

		get ParamTooltip() {
			return this.IsReadonly ? this.$scope.param.value : "";
		}

		get WidgetType(): string {
			switch (this.$scope.param.type) {
				case ParameterType.Enum:
				case ParameterType.Bool:
				case ParameterType.Call:
					return "select";
				default:
					return "string";
			}
		}

		Choices: IOption[];

		private MakeChoices() {
			const tuples = [];
			const options = this.$scope.param.options || [];
			let group: string;
			if (this.$scope.param.type === ParameterType.Call) {
				group = "Calls";
			} else {
				group = "Choices";
			}

			options.forEach(item => {
				const option: IOption = {
					key: item,
					text: item || "<i>Undefined</i>",
					description: "",
					group: group
				};
				if (item === this.$scope.param.defaultValue) {
					option.group = "Default";
					tuples.unshift(option);
				} else {
					tuples.push(option);
				}
			});
			this.Choices = tuples.concat(this.Defines());
		}

		private Defines(): IOption[] {
			return _.map(this.pitService.Pit.config, param => {
				const key = `##${param.key}##`;
				return <IOption> {
					key: key,
					text: key,
					description: param.description,
					group: "Defines"
				};
			});
		}
	}
}
