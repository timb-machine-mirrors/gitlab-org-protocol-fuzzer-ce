 /// <reference path="../reference.ts" />

module Peach.Models {
	"use strict";

	export interface IFault {
		id: string;
		faultUrl: string;
		jobUrl: string;
		targetUrl: string;
		pitUrl: string;
		nodeUrl: string;
		peachUrl: string;
		title: string;
		description: string;
		source: string;
		reproducable: boolean;
		iteration: number;
		seed: number;
		faultType: string;
		exploitability: string;
		majorHash: string;
		minorHash: string;
		folderName: string;
		timeStamp: string;
		bucketName: string;
		files: IFaultFile[];
	}

	export interface IFaultFile {
		name: string;
		fullName: string;
		fileUrl: string;
		size: number;
	}
}
