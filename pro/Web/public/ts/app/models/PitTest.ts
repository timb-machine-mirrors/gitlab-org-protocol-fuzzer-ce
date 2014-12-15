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
		public static Active: string = "active";
		public static Pass: string = "pass";
		public static Fail: string = "fail";
	}
}
