// Type definitions for react-bootstrap 

declare module "react-bootstrap" {
	import { ComponentClass, Props, ReactElement } from 'react';

	interface BreadcrumbProps extends Props<Breadcrumb> {
	}

	interface Breadcrumb extends ComponentClass<BreadcrumbProps> { }
	const Breadcrumb: Breadcrumb;

	interface BreadcrumbItemProps extends Props<BreadcrumbItem> {
		active?: boolean;
		href?: string;
		id?: string | number;
		linkId?: string | number;
		target?: string;
		title?: any;
	}

	interface BreadcrumbItem extends ComponentClass<BreadcrumbItemProps> { }
	const BreadcrumbItem: BreadcrumbItem;

	interface CollapseProps extends Props<Collapse> {
		dimension?: string | Function;
		getDimensionValue?: Function;
		in?: boolean;
		onEnter?: Function;
		onEntered?: Function;
		onEntering?: Function;
		onExit?: Function;
		onExited?: Function;
		onExiting?: Function;
		role?: string;
		timeout?: number;
		transitionAppear?: boolean;
		unmountOnExit?: boolean;
	}

	interface Collapse extends ComponentClass<CollapseProps> {
	}
	const Collapse: Collapse;


	interface InputProps extends Props<InputClass> {
		autoFocus?: boolean;
	}
}
