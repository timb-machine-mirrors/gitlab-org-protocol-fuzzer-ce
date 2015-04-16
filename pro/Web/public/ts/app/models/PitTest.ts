/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface ITestResult {
		status: string;
		events: ITestEvent[];
		log: string;
	}

	export interface ITestEvent {
		id: number;
		status: string;
		short?: string;
		description: string;
		resolve: string;
	}

	export module TestStatus {
		export var Active = "active";
		export var Pass = "pass";
		export var Fail = "fail";
	}
}
