/// <reference path="reference.ts" />

"use strict";

interface String {
	startsWith(str: string): boolean;
}

String.prototype.startsWith = function(pattern: string): boolean {
	return Peach.startsWith(this, pattern);
}

module Peach {

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

	export function isEmpty(value) {
		return _.isUndefined(value) || value === '' || value === null || value !== value;
	}

	export function startsWith(str: string, pattern: string): boolean {
		return str.slice(0, pattern.length) === pattern;
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
