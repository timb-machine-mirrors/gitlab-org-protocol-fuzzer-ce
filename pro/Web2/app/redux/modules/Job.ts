import superagent = require('superagent');
import { AWAIT_MARKER } from 'redux-await';
import { take, put, call, fork, cancel, CANCEL, SagaCancellationException } from 'redux-saga'

import RootState, { GetState } from '../../models/Root';
import { Job, JobStatus, JobRequest } from '../../models/Job';
import { MakeEnum, wait } from '../../utils';
import { fetchJobs } from './JobList';
import { fetchFaults, resetFaults } from './FaultList';

const POLL_INTERVAL = 3000;

const types = {
	JOB_POLL_START: '',
	JOB_POLL_STOP: '',
	JOB_FETCH: '',
	JOB_CMD_REQUEST: '',
	JOB_CMD_EXECUTE: '',
	JOB_DELETE: ''
};
MakeEnum(types);

const initial: Job = {
	id: null,
	pitUrl: null,
	startDate: null,
	speed: null,
	seed: null,
	iterationCount: null,
	faultCount: null
};

function update(state: Job, fragment: Job): Job {
	return Object.assign({}, state, fragment);
}

export default function reducer(state: Job = initial, action): Job {
	switch (action.type) {
		case types.JOB_FETCH:
			return onReceive(state, action);
		case types.JOB_CMD_REQUEST:
			return update(state, {
				status: action.request.status
			});
		case types.JOB_DELETE:
			return initial;
		default:
			return state;
	}
}

export function* saga(getState: GetState) {
	yield fork(watchDelete, getState);
	yield fork(watchPoll, getState);
	yield fork(watchCommand, getState);
}

export function startPolling(id: string) {
	return {
		type: types.JOB_POLL_START,
		id
	}
}

export function stopPolling() {
	return {
		type: types.JOB_POLL_STOP
	}
}

function fetchJob(id: string) {
	return {
		type: types.JOB_FETCH,
		AWAIT_MARKER,
		payload: {
			job: doFetch(id)
		}
	};
}

function commandRequest(status: string, url: string) {
	return {
		type: types.JOB_CMD_REQUEST,
		request: {
			status,
			url
		}
	}
}

function commandExecute(url: string) {
	return {
		type: types.JOB_CMD_EXECUTE,
		AWAIT_MARKER,
		payload: {
			job: doCommand(url)
		}
	}
}

export function stopJob(job: Job) {
	return commandRequest(JobStatus.StopPending, job.commands.stopUrl);
}

export function pauseJob(job: Job) {
	return commandRequest(JobStatus.PausePending, job.commands.pauseUrl);
}

export function continueJob(job: Job) {
	return commandRequest(JobStatus.ContinuePending, job.commands.continueUrl);
}

export function killJob(job: Job) {
	return commandRequest(JobStatus.KillPending, job.commands.killUrl);
}

export function deleteJob(job: Job) {
	return {
		type: types.JOB_DELETE,
		AWAIT_MARKER,
		payload: {
			job: doDelete(job)
		}
	}
}

function* watchDelete() {
	while (true) {
		yield take(types.JOB_DELETE);
		yield put(fetchJobs());
	}
}

function* watchCommand() {
	while (true) {
		const { request } = yield take(types.JOB_CMD_REQUEST);
		yield put(commandExecute(request.url));
	}
}

function* watchPoll(getState: GetState) {
	while (true) {
		const { id } = yield take(types.JOB_POLL_START);
		yield put(resetFaults());
		const task = yield fork(poll, getState, id);
		yield take(types.JOB_POLL_STOP);
		yield cancel(task);
	}
}

function* poll(getState: GetState, id: string) {
	try {
		while (true) {
			yield put(fetchJob(id));

			const action = yield take(types.JOB_FETCH);
			const job: Job = action.payload.job;

			const { faults } = getState();
			if (faults.isPolling && faults.data.length !== job.faultCount)
				yield put(fetchFaults(job));

			if (job.status === JobStatus.Stopped) {
				break;
			}
			yield call(wait, POLL_INTERVAL);
		}
	} catch (error) {
		if (!(error instanceof SagaCancellationException))
			throw error;
	}
}

function doFetch(id: string) {
	return new Promise<Job>((resolve, reject) => {
		superagent.get(`/p/jobs/${id}`)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Job failed to load: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}

function doDelete(job: Job) {
	return new Promise((resolve, reject) => {
		superagent.delete(job.jobUrl)
			.end((err, res) => {
				if (err) {
					reject(`Job failed to delete: ${err.message}`);
				} else {
					resolve();
				}
			})
		;
	});
}

function doCommand(url: string) {
	return new Promise<Job>((resolve, reject) => {
		superagent.get(url)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Job command failed: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}

function onReceive(cur: Job, action): Job {
	const next: Job = action.payload.job;

	const stopPending = (cur && cur.status === JobStatus.StopPending);
	const killPending = (cur && cur.status === JobStatus.KillPending);

	if (next.status !== JobStatus.Stopped) {
		if (stopPending && next.status !== JobStatus.Stopping) {
			next.status = JobStatus.StopPending;
		} else if (killPending) {
			next.status = JobStatus.KillPending;
		}
	}

	return next;
}
