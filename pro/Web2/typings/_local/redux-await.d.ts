// Type definitions for redux-await v5.0.0

/// <reference path="../redux/redux.d.ts" />

declare module 'redux-await/lib/constants' {
	export const AWAIT_MARKER: string;
	export const AWAIT_META_CONTAINER: string;
} 

declare module 'redux-await/lib/middleware' {
	import { Middleware } from 'redux';
	export function getPendingActionType(type): string;
	export function getFailedActionType(type): string;
	export function middleware(): Middleware;
}

declare module 'redux-await/lib/reducer' {
	export default function(state, action);
}

declare module 'redux-await/lib/connect' {
	export default function(mapStateToProps, ...args);
}

declare module 'redux-await' {
	export { AWAIT_MARKER, AWAIT_META_CONTAINER } from 'redux-await/lib/constants';
	export { middleware, getPendingActionType, getFailedActionType } from 'redux-await/lib/middleware';
	import reducer from 'redux-await/lib/reducer';
	import connect from 'redux-await/lib/connect';

	export {
		reducer,
		connect
	}
}
