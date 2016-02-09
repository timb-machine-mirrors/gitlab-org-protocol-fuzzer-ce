import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import superagent = require('superagent');

import { Pit } from '../../models/Pit';
import { Job, JobStatus, JobRequest } from '../../models/Job';
import { TestState, TestStatus, TestResult } from '../../models/PitTest';
import RootState from '../../models/Root';
import { MakeEnum } from '../../utils';

const POLL_INTERVAL = 1000;

const types = {
	TEST_START: '',
	TEST_POLL: '',
	TEST_STOP: '',
	TEST_TIMER: ''
};
MakeEnum(types);

const initial: TestState = {
	job: null,
	result: null,
	timer: null,
	isPending: false
};

export default function reducer(state: TestState = initial, action): TestState {
	switch (action.type) {
		case types.TEST_START:
			return Object.assign({}, state, {
				job: action.payload.test,
				isPending: true
			});
		case types.TEST_POLL:
			return onPoll(state, action);
		case types.TEST_STOP:
			return Object.assign({}, state, {
				timer: null
			});
		case types.TEST_TIMER:
			return Object.assign({}, state, {
				timer: action.timer
			});
		default:
			return state;
	}
}

export function startTest(pit: Pit) {
	return (dispatch: Dispatch, getState: Function) => {
		const state: RootState = getState();
		if (!_.isNull(state.test.timer))
			clearTimeout(state.test.timer);

		dispatch({
			type: types.TEST_START,
			AWAIT_MARKER,
			payload: {
				test: doStart(dispatch, pit)
			}
		});
	};
}

export function stopTest() {
	return (dispatch: Dispatch, getState: Function) => {
		const state: RootState = getState();
		if (!_.isNull(state.test.timer))
			clearTimeout(state.test.timer);

		dispatch({
			type: types.TEST_STOP,
			AWAIT_MARKER,
			payload: {
				test: doStop(dispatch)
			}
		});
	};
}

function doStart(dispatch: Dispatch, pit: Pit) {
	const request: JobRequest = {
		pitUrl: pit.pitUrl,
		dryRun: true
	};
	return new Promise<Job>((resolve, reject) => {
		superagent.post('/p/jobs')
			.accept('json')
			.send(request)
			.end((err, res) => {
				if (err) {
					reject(`Test failed to start: ${err.message}`);
				} else {
					const job: Job = res.body;
					resolve(job);
					dispatchPoll(dispatch, job);
				}
			})
		;
	});
}

function doStop(dispatch: Dispatch) {
	return new Promise<Job>((resolve, reject) => {
	});
}

function dispatchPoll(dispatch: Dispatch, job: Job) {
	dispatch({
		type: types.TEST_POLL,
		AWAIT_MARKER,
		payload: {
			test: doPoll(dispatch, job)
		}
	});
}

function doPoll(dispatch: Dispatch, job: Job) {
	return new Promise<TestResult>((resolve, reject) => {
		superagent.get(job.firstNodeUrl)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Test result failed to load: ${err.message}`);
				} else {
					const result: TestResult = res.body;
					resolve(result);
					if (result.status === TestStatus.Active) {
						doTimerStart(dispatch, job);
					} else {
						superagent.delete(job.jobUrl).end();
					}
				}
			})
		;
	});
}

function doTimerStart(dispatch: Dispatch, job: Job) {
	const timer = setTimeout(() => dispatchPoll(dispatch, job), POLL_INTERVAL);
	dispatch({
		type: types.TEST_TIMER,
		timer
	});
}

function onPoll(state: TestState, action) {
	const result: TestResult = action.payload.test;
	const isPending = (result.status === TestStatus.Active);
	const timer = null;
	return Object.assign({}, state, {
		result,
		isPending,
		timer
	});
}
