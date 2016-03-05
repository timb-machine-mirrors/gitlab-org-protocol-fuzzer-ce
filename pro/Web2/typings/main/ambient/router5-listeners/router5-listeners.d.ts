// Compiled using typings@0.6.10
// Source: https://raw.githubusercontent.com/flaub/typescript-definitions/master/router5-listeners/router5-listeners.d.ts
// Type definitions for router5-listeners v3.0.0

declare module "router5-listeners" {
	function listenersPlugin(options?: {});
	export default listenersPlugin;
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