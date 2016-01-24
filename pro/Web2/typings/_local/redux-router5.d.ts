// Type definitions for redux-router5 v2.0.0

declare module "redux-router5/modules/router5Middleware" {
	import Router5 from 'router5';
	import { Middleware } from 'redux';
	function router5ReduxMiddleware(router: Router5): Middleware;
	export default router5ReduxMiddleware;
}

declare module "redux-router5/modules/router5Reducer" {
	import { Reducer } from 'redux';
	const router5Reducer: Reducer;
	export default router5Reducer;
}

declare module "redux-router5/modules/routeNodeSelector" {
	function routeNodeSelector(routeNode, reducerKey?);
	export default routeNodeSelector;
}

declare module "redux-router5/modules/actions" {
	export function navigateTo(name, params?, opts?);
	export function cancelTransition();
	export function clearErrors();
	export function transitionStart(route, previousRoute);
	export function transitionSuccess(route, previousRoute);
	export function transitionError(route, previousRoute, transitionError);
}

declare module "redux-router5/modules/actionTypes" {
	export const NAVIGATE_TO;
	export const CANCEL_TRANSITION;
	export const TRANSITION_ERROR;
	export const TRANSITION_SUCCESS;
	export const TRANSITION_START;
	export const CLEAR_ERRORS;
}

declare module "redux-router5" {
	import router5Middleware from 'redux-router5/modules/router5Middleware';
	import router5Reducer from 'redux-router5/modules/router5Reducer';
	import routeNodeSelector from 'redux-router5/modules/routeNodeSelector';
	import * as actions from 'redux-router5/modules/actions';
	import * as actionTypes from 'redux-router5/modules/actionTypes';

	export {
		router5Middleware,
		router5Reducer,
		actions,
		actionTypes,
		routeNodeSelector
	};
}
