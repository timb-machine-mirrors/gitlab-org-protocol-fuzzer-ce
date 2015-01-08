/// <reference path="reference.ts" />

"use strict";

interface String {
	startsWith(str: string): boolean;
	endsWith(str: string): boolean;
}

String.prototype.startsWith = function (prefix: string): boolean {
	return this.slice(0, prefix.length) === prefix;
}

String.prototype.endsWith = function (suffix: string): boolean {
	return this.indexOf(suffix, this.length - suffix.length) !== -1;
}

module Peach {
	export interface IComponent {
		ComponentID: string;
	}

	export interface IDirective extends ng.IDirective, IComponent {
	}

	export interface ITab {
		title: string;
		content: string;
		active: boolean;
		disabled: boolean;
	}

	export interface IViewModelScope extends ng.IScope {
		vm: any;
	}

	export interface IFormScope extends IViewModelScope {
		form: ng.IFormController;
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

	export function ExtractId(part: string, url: string): string {
		var pattern = '/p/' + part + '/([^/]+).*';
		var re = new RegExp(pattern);
		return re.exec(url)[1];
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
}
