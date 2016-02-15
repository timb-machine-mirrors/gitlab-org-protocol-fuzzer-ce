﻿import { MakeLowerEnum } from '../utils';
import { Job } from './Job';

export interface TestState {
	isPending?: boolean;
	job?: Job;
	result?: TestResult;
}

export interface TestResult {
	status: string;
	events: TestEvent[];
	log: string;
}

export interface TestEvent {
	id: number;
	status: string;
	short?: string;
	description: string;
	resolve: string;
}

export var TestStatus = {
	Active : '',
	Pass   : '',
	Fail   : ''
};
MakeLowerEnum(TestStatus);
