/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface ITestRequest {
		pitUrl: string;
	}

	export interface ITestRef {
		testUrl: string;
	}

	export interface ITestResult {
		status: string;
		log: string;
		events: ITestEvent[];
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
