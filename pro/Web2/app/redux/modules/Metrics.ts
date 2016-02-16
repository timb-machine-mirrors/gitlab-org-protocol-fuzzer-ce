import { AWAIT_MARKER } from 'redux-await';

import { MetricsState } from '../../models/Metrics';
import { Job } from '../../models/Job';
import { MakeEnum } from '../../utils';
import { api } from '../../services';

const types = {
	METRICS_FETCH: ''
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

export function fetchMetric(job: Job, metric: string) {
	return {
		type: types.METRICS_FETCH,
		AWAIT_MARKER,
		payload: {
			metrics: api.fetchMetric(job, metric)
		}
	};
}

function onReceive(state: MetricsState, action): MetricsState {
	const { metrics } = action.payload;
	const data = metrics.data.map((item, index) => Object.assign({}, item, { id: index }));
	return Object.assign({}, state, {
		[metrics.metric]: data
	});
}
