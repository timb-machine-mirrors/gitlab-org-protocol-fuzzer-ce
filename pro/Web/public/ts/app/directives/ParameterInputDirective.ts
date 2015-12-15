/// <reference path="../reference.ts" />

namespace Peach {
	export const ParameterInputDirective: IDirective = {
		ComponentID: C.Directives.ParameterInput,
		restrict: "E",
		templateUrl: C.Templates.Directives.ParameterInput,
		controller: C.Controllers.ParameterInput,
		scope: {
			param: "=",
			form: "="
		}
	}

	export const ParameterComboDirective: IDirective = {
		ComponentID: C.Directives.ParameterCombo,
		restrict: 'E',
		require: [C.Directives.ParameterCombo, C.Angular.ngModel],
		controller: C.Controllers.Combobox,
		controllerAs: 'vm',
		templateUrl: C.Templates.Directives.ParameterCombo,
		scope: {
			data: '=',
			description: '=',
			placeholder: '='
		},
		link: (
			scope: IComboboxScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrls: any
		) => {
			const ctrl: ComboboxController = ctrls[0];
			ctrl.Link(element, attrs, ctrls[1]);
		}
	}

	export const ParameterSelectDirective: IDirective = {
		ComponentID: C.Directives.ParameterSelect,
		restrict: "E",
		templateUrl: C.Templates.Directives.ParameterSelect,
		controller: C.Controllers.ParameterInput,
		scope: {
			param: "=",
			form: "="
		}
	}

	export const ParameterStringDirective: IDirective = {
		ComponentID: C.Directives.ParameterString,
		restrict: "E",
		templateUrl: C.Templates.Directives.ParameterString,
		controller: C.Controllers.ParameterInput,
		scope: {
			param: "=",
			form: "="
		}
	}

	interface IOption {
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
				case ParameterType.Hwaddr:
				case ParameterType.Iface:
				case ParameterType.Ipv4:
				case ParameterType.Ipv6:
					return "combo";
				case ParameterType.Space:
					return "space";
				default:
					return "string";
			}
		}

		private Choices: IOption[];

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
