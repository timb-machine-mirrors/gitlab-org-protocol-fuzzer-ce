/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IUniqueScope extends ng.IScope {
		unique: Function;
		watch: string;
		channel: string;
		defaultValue: any;
		ctrl: UniqueChannelController;
		ngModel: ng.INgModelController;
		ignore: string;
	}

	export var UniqueDirective: IDirective = {
		ComponentID: Constants.Directives.Unique,
		restrict: 'A',
		require: Constants.Angular.ngModel,
		scope: {
			unique: '&' + Constants.Directives.Unique,
			watch: '@peachUniqueWatch',
			defaultValue: '@peachUniqueDefault'
		},
		link(
			scope: IUniqueScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			var validate = value => {
				var collection = scope.unique();
				var isUnique = !_.contains(collection, value || scope.defaultValue);
				ctrl.$setValidity('unique', isUnique);
				return value;
			};

			ctrl.$parsers.unshift(validate);
			ctrl.$formatters.unshift(validate);

			if (scope.watch) {
				scope.$watch(scope.watch, (newVal, oldVal) => {
					if (newVal !== oldVal) {
						validate(ctrl.$viewValue);
					}
				});
			}
		}
	}

	export var UniqueChannelDirective: IDirective = {
		ComponentID: Constants.Directives.UniqueChannel,
		restrict: 'A',
		require: Constants.Angular.ngModel,
		controller: Constants.Controllers.UniqueChannel,
		controllerAs: 'ctrl',
		scope: {
			channel: '@' + Constants.Directives.UniqueChannel,
			defaultValue: '@peachUniqueDefault',
			ignore: '@peachUniqueIgnore'
		},

		link: (
			scope: IUniqueScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			scope.ctrl.Link(element, attrs, ctrl);
		}
	}

	export class UniqueChannelController {
		static $inject = [Constants.Angular.$scope, Constants.Services.Unique];

		constructor(
			private $scope: IUniqueScope,
			private service: UniqueService
		) {
		}

		public Link(
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			this.$scope.ngModel = ctrl;

			this.service.Register(this.$scope);

			var validate = value => {
				this.service.IsUnique(this.$scope, value);
				return value;
			}

			ctrl.$formatters.unshift(validate);
			ctrl.$viewChangeListeners.unshift(() => validate(ctrl.$viewValue));

			element.on('$destroy', () => {
				this.service.Unregister(this.$scope);
			});
		}
	}

	class UniqueChannel {
		[id: string]: IUniqueScope;
	}

	class UniqueChannels {
		[name: string]: UniqueChannel
	}

	export class UniqueService {
		public IsUnique(scope: IUniqueScope, value: any): boolean {
			var isUnique;
			var channel = this.getChannel(scope.channel);
			_.forEach(channel, (item: IUniqueScope, id: string) => {
				var isDuplicate = this.isDuplicate(item, item.ngModel.$modelValue);
				item.ngModel.$setValidity('unique', !isDuplicate);
				if (id === scope.$id.toString()) {
					isUnique = !isDuplicate;
				}
			});
			return isUnique;
		}

		public Register(scope: IUniqueScope) {
			var channel = this.getChannel(scope.channel);
			channel[scope.$id.toString()] = scope;
		}

		public Unregister(scope: IUniqueScope) {
			var channel = this.getChannel(scope.channel);
			delete channel[scope.$id.toString()];
			if (_.isEmpty(channel)) {
				delete this.channels[scope.channel];
			} else {
				this.IsUnique(scope, null);
			}
		}

		private isDuplicate(scope: IUniqueScope, value: any): boolean {
			var myValue = (value || scope.defaultValue);
			if (scope.ignore) {
				var reIgnore: RegExp = new RegExp(scope.ignore);
				if (reIgnore.test(myValue)) {
					return false;
				}
			} 

			var channel = this.getChannel(scope.channel);
			return _.some(channel, (other: IUniqueScope, id: string) => {
				if (scope.$id.toString() === id) {
					return false;
				}
				var otherValue = other.ngModel.$modelValue;
				return (myValue === (otherValue || other.defaultValue));
			});
		}

		private getChannel(name: string): UniqueChannel {
			var channel = this.channels[name];
			if (_.isUndefined(channel)) {
				channel = {};
				this.channels[name] = channel;
			}
			return channel;
		}

		private channels: UniqueChannels = {};
	}
}
