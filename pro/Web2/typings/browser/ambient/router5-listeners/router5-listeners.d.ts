// Compiled using typings@0.6.8
// Source: https://raw.githubusercontent.com/flaub/typescript-definitions/master/router5-listeners/router5-listeners.d.ts
// Type definitions for router5-listeners v2.0.0

declare module "router5-listeners" {
	function listenersPlugin(options?: {});
	export = listenersPlugin;
}

declare module 'router5' {
	interface Router5 {
		addListener(fn: Function);
		removeListener(fn: Function);
		addNodeListener(nodeName: string, fn: Function);
		removeNodeListener(nodeName: string, fn: Function);
		addRouteListener(routeName: string, fn: Function);
		removeRouteListener(routeName: string, fn: Function);
	}
}