/// <reference path="../reference.ts" />

namespace Peach {
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

	export namespace TestStatus {
		export const Active = "active";
		export const Pass = "pass";
		export const Fail = "fail";
	}
}
