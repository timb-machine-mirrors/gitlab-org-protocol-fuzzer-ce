// Type definitions for router5 v2.0.1

declare module "route-node" {
	class RouteNode {
		constructor(name?: string, path?: string, childRoutes?: RouteNode[], cb?: Function);
	}

	export default RouteNode;
}

declare module "router5/modules/router5" {
	import RouteNode from "route-node";

	interface Options {
		useHash?: boolean;
		defaultRoute?: any;
		defaultParams?: any;
	}
	
	type Routes = RouteNode[] | {}[] | RouteNode | {};

	class Router5 {
		constructor(routes?: Routes, opts?: Options);
		setOption(opt: string, val: any): Router5;
		setAdditionalArgs(args: any[]): void;
		getAdditionalArgs(): any[];
		add(routes: Routes): Router5;
		addNode(name: string, path: string, canActivate?: Function): Router5;
		usePlugin(pluginFactory: Function): Router5;
		useMiddleware(...fn: Function[]): Router5;
		start(startPathOrState?: string | {}, done?: Function): Router5;
		stop(): Router5;
		getState(): any;
		isActive(name: string, params?: {}, strictEquality?: boolean, ignoreQueryParams?: boolean): boolean;
		areStatesDescendants(parentState: {}, childState: {}): boolean;
		canDeactivate(name: string, canDeactivate: boolean): Router5;
		canActivate(name: string, canActivate: boolean): Router5;
		buildUrl(route: string, params?: {}): string;
		buildPath(route: string, params?: {}): string;
		buildState(route: string, params?: {}): {};
		matchPath(path: string): {};
		urlToPath(url: string): string;
		matchUrl(url: string): {};
		navigate(name: string, params?: {}, opts?: {}, done?: Function): Function;

		// HACK: can not get declaration merging to work!
		//       would be better to define these in router5-listeners.d.ts
		addListener(fn: Function);
		removeListener(fn: Function);
		addNodeListener(nodeName: string, fn: Function);
		removeNodeListener(nodeName: string, fn: Function);
		addRouteListener(routeName: string, fn: Function);
		removeRouteListener(routeName: string, fn: Function);
	}

	export default Router5;
}

declare module "router5/modules/logger" {
	function loggerPlugin();
	export default loggerPlugin;
}

declare module "router5.transition-path" {
	function transitionPath(toState, fromState);
	export default transitionPath;
}

declare module "router5/modules/constants" {
	namespace constants {
		const ROUTER_NOT_STARTED;
		const ROUTER_ALREADY_STARTED;
		const ROUTE_NOT_FOUND;
		const SAME_STATES;
		const CANNOT_DEACTIVATE;
		const CANNOT_ACTIVATE;
		const TRANSITION_ERR;
		const TRANSITION_CANCELLED;
	}

	export default constants;
}

declare module "router5" {
	import Router5 from "router5/modules/router5";
	import RouteNode from "route-node";
	import loggerPlugin from "router5/modules/logger";
	import transitionPath from 'router5.transition-path';
	import errCodes from "router5/modules/constants";

	export default Router5;

	export { 
		Router5,
		RouteNode,
		loggerPlugin,
		errCodes,
		transitionPath
	}
}
