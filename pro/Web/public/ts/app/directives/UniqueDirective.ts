/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IUniqueScope extends ng.IScope {
		unique: Function;
		watch: string;
		channel: string;
		defaultValue: any;
		ctrl: ng.INgModelController;
		ignore: string;
	}

	export class UniqueDirective implements ng.IDirective {
		public restrict = 'A';
		public require = 'ngModel';
		public scope = {
			unique: '&peachUnique',
			watch: '@peachUniqueWatch',
			defaultValue: '@peachUniqueDefault'
		};

		public link(
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

	export class UniqueChannelDirective implements ng.IDirective {
		constructor(
			private service: UniqueService
		) {
			this.link = this._link.bind(this);
		}

		public restrict = 'A';
		public require = 'ngModel';
		public scope = {
			channel: '@peachUniqueChannel',
			defaultValue: '@peachUniqueDefault',
			ignore: '@peachUniqueIgnore'
		};

		public link: ng.IDirectiveLinkFn;

		private _link(
			scope: IUniqueScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			scope.ctrl = ctrl;

			this.service.Register(scope);

			var validate = value => {
				this.service.IsUnique(scope, value);
				return value;
			}

			ctrl.$formatters.unshift(validate);
			ctrl.$viewChangeListeners.unshift(() => validate(ctrl.$viewValue));

			element.on('$destroy', () => {
				this.service.Unregister(scope);
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
				var isDuplicate = this.isDuplicate(item, item.ctrl.$modelValue);
				item.ctrl.$setValidity('unique', !isDuplicate);
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
				var otherValue = other.ctrl.$modelValue;
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
