import superagent = require('superagent');
import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import { take, put, call } from 'redux-saga/effects';

import RootState, { GetState } from '../../models/Root';
import { Pit } from '../../models/Pit';
import { Job, JobStatus, JobRequest } from '../../models/Job';
import { TestState, TestStatus, TestResult } from '../../models/PitTest';
import { MakeEnum, wait } from '../../utils';

const POLL_INTERVAL = 1000;

const types = {
	TEST_START: '',
	TEST_STOP: '',
	TEST_FETCH: '',
	TEST_DELETE: '',
	TEST_RESET: ''
};
MakeEnum(types);

const initial: TestState = {
	isPending: false,
	job: null,
	result: null
};

function update(state: TestState, fragment: TestState): TestState {
	return Object.assign({}, state, fragment);
}

export default function reducer(state: TestState = initial, action): TestState {
	switch (action.type) {
		case types.TEST_START:
			return update(state, {
				isPending: true,
				job: action.payload.testJob
			});
		case types.TEST_FETCH:
			return onFetch(state, action);
		case types.TEST_DELETE:
			return update(state, {
				job: null
			});
		case types.TEST_RESET:
			return initial;
		default:
			return state;
	}
}

export function* saga(getState: GetState) {
	while (true) {
		const action = yield take(types.TEST_START);
		const job: Job = action.payload.testJob;
		yield call(poll, job);
		yield put(deleteJob(job));
	}
}

export function startTest(pit: Pit) {
	return {
		type: types.TEST_START,
		AWAIT_MARKER,
		payload: {
			testJob: doStart(pit)
		}
	};
}

export function stopTest(job: Job) {
	return {
		type: types.TEST_START,
		AWAIT_MARKER,
		payload: {
			testJob: doStop(job)
		}
	};
}

export function resetTest() {
	return { type: types.TEST_RESET };
}

function* poll(job: Job) {
	while (true) {
		yield put(fetchResult(job));
		const action = yield take(types.TEST_FETCH);
		const result: TestResult = action.payload.testResult;
		if (result.status !== TestStatus.Active) {
			break;
		}
		yield call(wait, POLL_INTERVAL);
	}
}

function doStart(pit: Pit) {
	const request: JobRequest = {
		pitUrl: pit.pitUrl,
		dryRun: true
	};
	return new Promise<Job>((resolve, reject) => {
		superagent.post('/p/jobs')
			.type('json')
			.accept('json')
			.send(request)
			.end((err, res) => {
				if (err) {
					reject(`Test failed to start: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}

function doStop(job: Job) {
	return new Promise((resolve, reject) => {
		superagent.get(job.commands.stopUrl)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Test failed to start: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}

function fetchResult(job: Job) {
	return {
		type: types.TEST_FETCH,
		AWAIT_MARKER,
		payload: {
			testResult: doFetch(job)
		}
	};
}

function doFetch(job: Job) {
	return new Promise<TestResult>((resolve, reject) => {
		superagent.get(job.firstNodeUrl)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Test result failed to load: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}

function deleteJob(job: Job) {
	return {
		type: types.TEST_DELETE,
		AWAIT_MARKER,
		payload: {
			testDelete: doDelete(job)
		}
	};
}

function doDelete(job: Job) {
	return new Promise((resolve, reject) => {
		superagent.delete(job.jobUrl)
			.end((err, res) => {
				if (err) {
					reject(`Test job failed to delete: ${err.message}`);
				} else {
					resolve();
				}
			});
		;
	});
}

function onFetch(state: TestState, action) {
	const result: TestResult = action.payload.testResult;
	const isPending = (result.status === TestStatus.Active);
	return update(state, {
		isPending,
		result
	});
}
