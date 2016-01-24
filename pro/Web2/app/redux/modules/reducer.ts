import { combineReducers } from 'redux';
import { router5Reducer as router } from 'redux-router5';
import { reducer as form } from 'redux-form';
import { reducer as await } from 'redux-await';

import error from './Error';
import library from './Library';
import pit from './Pit';
import job from './Job';
import jobs from './JobList';
import faults from './FaultList';
import fault from './Fault';
import metrics from './Metrics';

const root = combineReducers({
	await,
	error,
	form,
	router,
	library,
	pit,
	job,
	jobs,
	faults,
	fault,
	metrics
});

export default root;
