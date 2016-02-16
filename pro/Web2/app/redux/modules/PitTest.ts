import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import { take, put, call } from 'redux-saga/effects';

import RootState, { GetState } from '../../models/Root';
import { Pit } from '../../models/Pit';
import { Job, JobStatus, JobRequest } from '../../models/Job';
import { TestState, TestStatus, TestResult } from '../../models/PitTest';
import { MakeEnum, wait } from '../../utils';
import { api } from '../../services';

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
			testJob: api.startTest(pit)
		}
	};
}

export function stopTest(job: Job) {
	return {
		type: types.TEST_START,
		AWAIT_MARKER,
		payload: {
			testJob: api.stopTest(job)
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

function fetchResult(job: Job) {
	return {
		type: types.TEST_FETCH,
		AWAIT_MARKER,
		payload: {
			testResult: api.fetchTestResult(job)
		}
	};
}

function deleteJob(job: Job) {
	return {
		type: types.TEST_DELETE,
		AWAIT_MARKER,
		payload: {
			testDelete: api.deleteJob(job)
		}
	};
}

function onFetch(state: TestState, action) {
	const result: TestResult = action.payload.testResult;
	const isPending = (result.status === TestStatus.Active);
	return update(state, {
		isPending,
		result
	});
}
