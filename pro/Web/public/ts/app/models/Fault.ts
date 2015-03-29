 /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IFaultLinks {
		nodeUrl: string;
		targetUrl: string;
		targetConfigUrl: string;
		pitUrl: string;
		peachUrl: string;
		archiveUrl: string;
	}

	export interface IFaultSummary {
		faultUrl: string;
		reproducable: boolean;
		iteration: number;
		timeStamp: string;
		bucketName: string;
		source: string;
		exploitability: string;
		majorHash: string;
		minorHash: string;
	}

	export interface IFaultDetail extends IFaultSummary {
		links: IFaultLinks;

		title: string;
		description: string;
		seed: number;
		files: IFaultFile[];

		// range of search when fault was found
		iterationStart: number;
		iterationStop: number;
	}

	export interface IFaultFile {
		name: string;
		fullName: string;
		fileUrl: string;
		size: number;
	}
}
