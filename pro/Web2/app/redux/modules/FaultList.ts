import superagent = require('superagent');
import { AWAIT_MARKER } from 'redux-await';
import { take, put, call, fork, cancel, SagaCancellationException } from 'redux-saga'

import RootState, { GetState } from '../../models/Root';
import { Job } from '../../models/Job';
import { FaultListState, FaultSummary } from '../../models/Fault';
import { MakeEnum } from '../../utils';

const types = {
	FAULTS_POLL_START: '',
	FAULTS_POLL_STOP: '',
	FAULTS_FETCH: '',
	FAULTS_RESET: ''
};
MakeEnum(types);

const initial: FaultListState = {
	isPolling: false,
	data: []
};

function update(state: FaultListState, fragment: FaultListState): FaultListState {
	return Object.assign({}, state, fragment);
}

export default function reducer(state: FaultListState = initial, action): FaultListState {
	switch (action.type) {
		case types.FAULTS_POLL_START:
			return update(state, {
				isPolling: true
			});
		case types.FAULTS_POLL_STOP:
			return update(state, {
				isPolling: false
			});
		case types.FAULTS_FETCH:
			return update(state, {
				data: action.payload.faults
			});
		case types.FAULTS_RESET:
			return initial;
		default:
			return state;
	}
}

export function startPolling() {
	return { type: types.FAULTS_POLL_START }
}

export function stopPolling() {
	return { type: types.FAULTS_POLL_STOP }
}

export function fetchFaults(job: Job) {
	return {
		type: types.FAULTS_FETCH,
		AWAIT_MARKER,
		payload: {
			faults: doFetch(job)
		}
	};
}

export function resetFaults() {
	return { type: types.FAULTS_RESET };
}

function doFetch(job: Job) {
	return new Promise<FaultSummary[]>((resolve, reject) => {
		superagent.get(job.faultsUrl)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Fault listing failed to load: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}
