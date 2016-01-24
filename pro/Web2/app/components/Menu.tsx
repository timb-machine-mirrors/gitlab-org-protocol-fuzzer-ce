import React = require('react');
import { Component, Props, MouseEvent, ReactNode } from 'react';
import Icon = require('react-fa');
import { connect } from 'react-redux';
import { bindActionCreators } from 'redux';
import { actions } from 'redux-router5';
import { Collapse } from 'react-bootstrap';

import { R, RouteSpec } from '../routes';
import { Route, RouterContext, injectRouter } from '../models/Router';
import { createLinkDescriptor, BaseLinkProps } from './Link';

export class Menu extends Component<Props<Menu>, {}> {
	render() {
		const { children } = this.props;
		return (
			<ul className="nav nav-list">
				{children}
				<MenuItem to={R.Docs} external />
				<MenuItem to={R.Forums} external />
			</ul>
		)
	}
}

interface MenuItemState {
	open: boolean;
}

interface MenuItemProps extends Props<MenuItem> {
	to?: RouteSpec;
	options?: {};
	external?: boolean;
	// injected
	route?: Route;
	navigateTo?: Function;
}

@connect(
	state => ({ route: state.router.route }),
	dispatch => bindActionCreators({ navigateTo: actions.navigateTo }, dispatch)
)
@injectRouter
export class MenuItem extends Component<MenuItemProps, MenuItemState> {
	context: RouterContext;

	constructor(props?: MenuItemProps, context?: any) {
		super(props, context);
		this.state = this.computeState(this.props);
	}

	componentWillReceiveProps(next: MenuItemProps) {
		this.setState(this.computeState(next));
	}

	computeState(props: MenuItemProps) {
		const { router } = this.context;
		const { to, route } = props;
		const active = router.isActive(to.name, route.params);
		return { open: active };
	}

	render() {
		return this.props.external ? this.renderExternal() : this.renderInternal();
	}

	renderInternal() {
		return this.props.children ? this.renderBranch() : this.renderLeaf();
	}

	renderLeaf() {
		const { to, route, children } = this.props;
		const label = to.label || to.displayName;
		const props = _.assign<{}, BaseLinkProps>({}, this.props, {
			params: route.params,
			isStrict: true
		});
		const { href, onClick, active } = createLinkDescriptor(this.context.router, props);
		const className = active ? 'active' : '';

		return (
			<li className={className}>
				<a href={href} onClick={onClick}>
					{to.icon && <Icon name={to.icon} />}
					<span className= "menu-text">{label}</span>
				</a>
			</li>
		)
	}

	renderBranch() {
		const { to, children } = this.props;
		const { open } = this.state;
		const label = to.label || to.displayName;
		const onClick = (evt: MouseEvent) => {
			evt.preventDefault();
			this.setState({
				open: !this.state.open
			});
		};

		return (
			<li>
				<a href="#" onClick={onClick}>
					<Icon name={to.icon} />
					<span className= "menu-text">{label}</span>
					<Icon name="angle-down" className="arrow" />
				</a>

				<Collapse in={open}>
					<ul className="submenu">
						{children}
					</ul>
				</Collapse>
			</li>
		)

	}

	renderExternal() {
		const { to } = this.props;
		return (
			<li>
				<a href={to.path} target="_blank">
					<Icon name={to.icon} />
					<span className="menu-text">{to.label}</span>
				</a>
			</li>
		)
	}
}
