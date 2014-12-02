/// <reference path="reference.ts" />

"use strict";

function onlyIf<T>(preds: any, fn: () => T): T {
	if (!_.isArray(preds)) {
		preds = [preds];
	}
	if (_.every(preds)) {
		return fn();
	}
	return undefined;
}

function startsWith(str: string, pattern: string): boolean {
	return str.slice(0, pattern.length) === pattern;
}

interface String {
	startsWith(str: string): boolean;
}

String.prototype.startsWith = function(pattern: string): boolean {
	return startsWith(this, pattern);
}

function isEmpty(value) {
	return _.isUndefined(value) || value === '' || value === null || value !== value;
}
