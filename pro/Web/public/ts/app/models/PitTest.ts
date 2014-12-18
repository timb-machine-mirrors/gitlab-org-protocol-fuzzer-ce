/// <reference path="../reference.ts" />

module Peach {
	"use strict";

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
		short: string;
		description: string;
		resolve: string;
	}

	export class TestStatus {
		static Active: string = "active";
		static Pass: string = "pass";
		static Fail: string = "fail";
	}
}
