import React = require('react');
import { Router5 } from 'router5';
import { Component, Props, EventHandler, MouseEvent, ReactType, createElement } from 'react';
import { connect } from 'react-redux';
import { bindActionCreators } from 'redux';
import { actions } from 'redux-router5';

import { injectRouter, RouterContext } from '../models/Router';
import { RouteSpec } from '../routes';

export interface BaseLinkProps {
	to: RouteSpec;
	params?: {};
	options?: {};
	isStrict?: boolean;
	// injected
	navigateTo?: Function;
}

interface LinkProps extends BaseLinkProps, Props<Link> {
	activeComponent?: ReactType;
}

interface LinkDescriptor {
	href: string;
	onClick: EventHandler<MouseEvent>;
	active: boolean;
}

export function createLinkDescriptor(router: Router5, props: BaseLinkProps): LinkDescriptor {
	const { to, params, options, navigateTo, isStrict } = props;
	return {
		href: router.buildUrl(to.name, params),
		active: router.isActive(to.name, params, isStrict),
		onClick: (evt: MouseEvent) => {
			evt.preventDefault();
			navigateTo(to.name, params, options);
		}
	};
}

@connect(
	state => ({}),
	dispatch => bindActionCreators({ navigateTo: actions.navigateTo }, dispatch)
)
@injectRouter
class Link extends Component<LinkProps, {}> {
	context: RouterContext;
	
	render() {
		const { activeComponent, children } = this.props;
		const { href, onClick, active } = createLinkDescriptor(this.context.router, this.props);
		const className = active ? 'active' : '';

		if (activeComponent) {
			return (
				createElement(activeComponent as string, { className: className },
					<a href={href} onClick={onClick}>
						{ children }
					</a>
				)
			)
		}

		return (
			<a href={href} onClick={onClick} className={className}>
				{ children }
			</a>
		)
	}
}

export default Link;
