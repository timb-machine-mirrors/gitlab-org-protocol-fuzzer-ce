import { combineReducers } from 'redux';
import { router5Reducer as router } from 'redux-router5';
import { reducer as form } from 'redux-form';
import { reducer as await } from 'redux-await';
import { fork } from 'redux-saga/effects';

import error from './Error';
import library from './Library';
import pit, { saga as pitSaga } from './Pit';
import test, { saga as testSaga } from './PitTest';
import job, { saga as jobSaga } from './Job';
import jobs from './JobList';
import faults from './FaultList';
import fault from './Fault';
import metrics from './Metrics';

export const rootReducer = combineReducers({
	await,
	error,
	form,
	router,
	library,
	pit,
	test,
	job,
	jobs,
	faults,
	fault,
	metrics
});

export function* rootSaga(getState) {
	yield fork(jobSaga, getState);
	yield fork(testSaga, getState);
	yield fork(pitSaga, getState);
}
