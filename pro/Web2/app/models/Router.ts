import { PropTypes } from 'react';
import { Router5 } from 'router5';

export interface Route {
	name: string;
	path: string;
	params: any;
	_meta: string[];
}

export interface RouterContext {
	router: Router5;
}

export interface RouterState {
	route: Route;
}

export function injectRouter(target: any) {
	target.contextTypes = target.contextTypes || {};
	target.contextTypes.router = PropTypes.object.isRequired;
}
