import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import superagent = require('superagent');

import { MetricsState } from '../../models/Metrics';
import { Job } from '../../models/Job';
import { MakeEnum } from '../../utils';

const types = {
	METRICS_FETCH: '',
};
MakeEnum(types);

const initial: MetricsState = {
	bucketTimeline: [],
	faultTimeline: [],
	mutators: [],
	elements: [],
	states: [],
	dataset: [],
	buckets: []
};

export default function reducer(state: MetricsState = initial, action): MetricsState {
	switch (action.type) {
		case types.METRICS_FETCH:
			return onReceive(state, action);
		default:
			return state;
	}
}

export function fetch(job: Job, metric: string) {
	return (dispatch: Dispatch, getState: Function) => {
		dispatch({
			type: types.METRICS_FETCH,
			AWAIT_MARKER,
			payload: {
				metrics: doFetch(job, metric)
			}
		});
	};
}

function doFetch(job: Job, metric: string) {
	return new Promise((resolve, reject) => {
		const url = job.metrics[metric];
		superagent.get(url)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Metric '${metric}' failed to load: ${err.message}`);
				} else {
					resolve({
						metric,
						data: res.body 
					});
				}
			})
		;
	});
}

function onReceive(state: MetricsState, action): MetricsState {
	const { metrics } = action.payload;
	const data = metrics.data.map((item, index) => Object.assign({}, item, { id: index }));
	return Object.assign({}, state, {
		[metrics.metric]: data
	});
}
