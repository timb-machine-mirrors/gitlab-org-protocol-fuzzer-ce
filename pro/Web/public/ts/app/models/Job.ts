﻿/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IJobCommands {
		stopUrl: string;
		continueUrl: string;
		pauseUrl: string;
		killUrl: string;
	}

	export interface IJobMetrics {
		[name: string]: string;
	}

	export interface IJob {
		// URL to configured pit
		// /p/pits/ID
		pitUrl: string;

		commands?: IJobCommands;

		jobUrl?: string;

		status?: string;
		mode?: string;

		// all faults generated by this job
		// "/p/jobs/0123456789AB/faults"
		faultsUrl?: string;

		// target we are fuzzing
		targetUrl?: string;

		// target configuration being used
		targetConfigUrl?: string;

		// fuzzing nodes used by job
		// "/p/jobs/0123456789AB/nodes";
		nodesUrl?: string;

		// version of peach used by job, fully qualified
		peachUrl?: string;

		//"/p/files/ID",
		reportUrl?: string;

		// grid fs url to package
		// "/p/files/...",
		packageFileUrl?: string;

		metrics?: IJobMetrics;

		// display name for job
		// "0123456789AB"
		name?: string;

		// ":"notes from user about job, optional",
		notes?: string;

		// Set to null for now
		user?: string;

		// 31337,
		seed?: number;

		// current iteration count
		iterationCount?: number;

		startDate?: Date;
		stopDate?: Date;

		// seconds we have been running
		runtime?: number;

		// iterations per hour
		speed?: number;

		// total number of faults
		faultCount?: number;

		// Always 1
		nodeCount?: number;

		// Set to 127.0.0.1
		ipAddress?: string;

		// Empty list
		tags?: ITag[];

		groups?: any[];

		rangeStart?: number;
		rangeStop?: number;

		result?: string;
	}

	export class JobStatus {
		public static StartPending: string = "startPending";
		public static Running: string = "running";
		public static PausePending: string = "pausePending";
		public static Paused: string = "paused";
		public static ContinuePending: string = "continuePending";
		public static StopPending: string = "stopPending";
		public static Stopped: string = "stopped";
		public static ActionPending: string = "actionPending";
	}

	export class JobMode {
		public static Fuzzing: string = "fuzzing";
		public static Searching: string = "searching";
		public static Reproducing: string = "reproducing";
	}
}
