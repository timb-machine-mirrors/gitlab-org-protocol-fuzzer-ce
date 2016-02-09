// Compiled using typings@0.6.6
// Source: https://raw.githubusercontent.com/flaub/typescript-definitions/master/react-router5/react-router5.d.ts
// Type definitions for react-router5 v2.0.0


declare module "react-router5/modules/BaseLink" {
	import { Router5 } from 'router5';
	import { ComponentClass, Props } from 'react';

	interface RouteOptions {
		reload?: boolean;
	}

	interface BaseLinkProps extends Props<BaseLink> {
		router: Router5;
		routeName: string;
		routeParams?: {};
		routeOptions?: RouteOptions;
		activeClassName?: string;
		activeStrict?: boolean;
		onClick?: Function;
	}

	interface BaseLink extends ComponentClass<BaseLinkProps> { }
	const BaseLink: BaseLink;

	export default BaseLink;
}

declare module "react-router5/modules/routeNode" {
	function routeNode(nodeName: string, register?: boolean): Function;
	export default routeNode;
}

declare module "react-router5/modules/RouterProvider" {
	import { Router5 } from 'router5';
	import { ComponentClass, Props, ReactNode } from 'react';
	
	interface RouterProviderProps extends Props<RouterProvider> {
		router: Router5;
	}
	
	interface RouterProvider extends ComponentClass<RouterProviderProps> { }
	const RouterProvider: RouterProvider;

	export default RouterProvider;
}

declare module "react-router5/modules/withRoute" {
	function withRoute<C>(BaseComponent: C): C;
	export default withRoute;
}

declare module "react-router5" {
	import { Router5 } from 'router5';
	import { ComponentClass, Props } from 'react';
	import BaseLink from 'react-router5/modules/BaseLink';
	import routeNode from 'react-router5/modules/routeNode';
	import RouterProvider from 'react-router5/modules/RouterProvider';
	import withRoute from 'react-router5/modules/withRoute';

	interface RouteOptions {
		reload?: boolean;
	}
	
	interface LinkProps extends Props<Link> {
		router?: Router5;
		routeName: string;
		routeParams?: {};
		routeOptions?: RouteOptions;
		activeClassName?: string;
		activeStrict?: boolean;
		onClick?: Function;
	}

	interface Link extends ComponentClass<LinkProps> { }
	const Link: Link;

	export {
		BaseLink,
		routeNode,
		RouterProvider,
		withRoute,
		Link
	};
}