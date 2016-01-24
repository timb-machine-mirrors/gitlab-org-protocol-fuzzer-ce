/// <reference path="../typings/tsd.d.ts"/>

import React = require('react');
import { render } from 'react-dom';
import { Provider } from 'react-redux';
import Router5, { loggerPlugin } from 'router5';
import { RouterProvider } from 'react-router5';
import historyPlugin = require('router5-history');
import listenersPlugin = require('router5-listeners');

import Main from './containers/Main';
import DevTools from './containers/DevTools';
import { R, routes } from './routes';
import createStore from './redux/create';

const router = new Router5(routes)
	.setOption('useHash', true)
	.setOption('defaultRoute', R.Root.name)
	.usePlugin(historyPlugin())
	.usePlugin(listenersPlugin())
	// .usePlugin(loggerPlugin())
	.addListener(() => {
		window.scrollTo(0, 0);
	})
;


router.start((err, state) => {
	const store = createStore(router, state);

	render(
		<Provider store={store}>
			<div>
				<RouterProvider router={router}>
					<Main />
				</RouterProvider>
				<DevTools />
			</div>
		</Provider>,
		document.getElementById('app')
	);
});
