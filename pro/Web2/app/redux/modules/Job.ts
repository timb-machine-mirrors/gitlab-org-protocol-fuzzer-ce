import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import superagent = require('superagent');

import { Job, JobStatus } from '../../models/Job';
import RootState from '../../models/Root';
import { MakeEnum } from '../../utils';

const JOB_INTERVAL = 3000;

const types = {
	JOB_FETCH: '',
	JOB_FETCH_TIMER_START: '',
	JOB_FETCH_TIMER_CLEAR: ''
};
MakeEnum(types);

const initial: Job = {
	id: null,
	pitUrl: null,
	startDate: null,
	speed: null,
	seed: null,
	iterationCount: null,
	faultCount: null,
	timer: null
};

export default function reducer(state: Job = initial, action): Job {
	switch (action.type) {
		case types.JOB_FETCH:
			return onReceive(state, action);
		case types.JOB_FETCH_TIMER_START:
			return Object.assign({}, state, {
				timer: action.timer
			});
		case types.JOB_FETCH_TIMER_CLEAR:
			if (state.timer)
				clearTimeout(state.timer);
			return Object.assign({}, state, {
				timer: null
			});
		default:
			return state;
	}
}

export function startPolling(id: string) {
	return (dispatch: Dispatch, getState: Function) => {
		const state: RootState = getState();
		if (state.job.timer)
			clearTimeout(state.job.timer);
		dispatchFetch(dispatch, id);
	};
}

export function stopPolling() {
	return (dispatch: Dispatch, getState: Function) => {
		dispatch({
			type: types.JOB_FETCH_TIMER_CLEAR
		});
	};
}

function dispatchFetch(dispatch: Dispatch, id: string) {
	dispatch({
		type: types.JOB_FETCH,
		AWAIT_MARKER,
		payload: {
			job: doFetch(dispatch, id)
		}
	});
}

function doFetch(dispatch: Dispatch, id: string) {
	return new Promise<Job>((resolve, reject) => {
		superagent.get(`/p/jobs/${id}`)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Job failed to load: ${err.message}`);
				} else {
					const job: Job = res.body;
					resolve(job);
					if (job.status !== JobStatus.Stopped) {
						doTimerStart(dispatch, id);
					}
				}
			})
		;
	});
}

function doTimerStart(dispatch: Dispatch, id: string) {
	const timer = setTimeout(() => dispatchFetch(dispatch, id), JOB_INTERVAL);
	dispatch({
		type: types.JOB_FETCH_TIMER_START,
		timer
	});
}

function onReceive(cur: Job, action): Job {
	const next = action.payload.job;
	next.timer = null;

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
