import { Router5 } from 'router5';
import { createStore, applyMiddleware, compose } from 'redux';
import { middleware as awaitMiddleware } from 'redux-await';
import { router5Middleware } from 'redux-router5';

import DevTools from '../containers/DevTools';
import root from './modules/reducer';
import thunk from './middleware/thunk';

function doCreateStore(router: Router5, state) {
	// const createLogger = require('redux-logger');
	// const logger = createLogger();

	const finalCreateStore = compose(
		applyMiddleware(
			thunk,
			awaitMiddleware,
			// logger,
			router5Middleware(router)
		),
		DevTools.instrument()
	)(createStore);

	return finalCreateStore(root, { router: { route: state } });
}

export default doCreateStore;
