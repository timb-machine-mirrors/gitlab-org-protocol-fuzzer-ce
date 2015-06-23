/// <reference path="reference.ts" />

"use strict";

interface String {
	startsWith(str: string): boolean;
	endsWith(str: string): boolean;
	format(...args: any[]): string;
	paddingLeft(pattern: string): string;
}

String.prototype.startsWith = function (prefix: string): boolean {
	return this.slice(0, prefix.length) === prefix;
}

String.prototype.endsWith = function (suffix: string): boolean {
	return this.indexOf(suffix, this.length - suffix.length) !== -1;
}

String.prototype.format = function (...args: any[]): string {
	return this.replace(/{(\d+)}/g,(match, i) => {
		return !_.isUndefined(args[i]) ? args[i] : match;
	});
}

String.prototype.paddingLeft = function (pattern: string): string {
	return String(pattern + this).slice(-pattern.length);
}

module Peach {
	export interface IComponent {
		ComponentID: string;
	}

	export interface IDirective extends ng.IDirective, IComponent {
	}
	
	export interface IRootScope extends ng.IScope {
		job: IJob;
		pit: IPit;
	}

	export interface IViewModelScope extends IRootScope {
		vm: any;
	}

	export interface IFormScope extends IViewModelScope {
		form: ng.IFormController;
	}
	
	export function onlyWith<T, R>(obj: T, fn: (T) => R): R {
		if (!_.isUndefined(obj)) {
			return fn(obj);
		}
		return undefined;
	}

	export function onlyIf<T>(preds: any, fn: () => T): T {
		if (!_.isArray(preds)) {
			preds = [preds];
		}
		if (_.every(preds)) {
			return fn();
		}
		return undefined;
	}

	export function ArrayItemUp<T>(array: T[], i: number): T[] {
		if (i > 0) {
			var x = array[i - 1];
			array[i - 1] = array[i];
			array[i] = x;
		}
		return array;
	}

	export function ArrayItemDown<T>(array: T[], i: number): T[] {
		if (i < array.length - 1) {
			var x = array[i + 1];
			array[i + 1] = array[i];
			array[i] = x;
		}
		return array;
	}

	export function StripHttpPromise<T>($q: ng.IQService, promise: ng.IHttpPromise<T>): ng.IPromise<T> {
		var deferred = $q.defer<T>();
		promise.success((data: T) => {
			deferred.resolve(data);
		});
		promise.error(reason => {
			deferred.reject(reason);
		});
		return deferred.promise;
	}
}
