import React = require('react');
import { Component, Props } from 'react';
import { Breadcrumb } from 'react-bootstrap';
import { connect } from 'react-redux';

import { Route, RouterContext, injectRouter } from '../models/Router';
import { DisplayNameFunc, RouteSpec, segments } from '../routes';
import Link from './Link';
import RootState from '../models/Root';

interface BreadcrumbsProps extends Props<Breadcrumbs> {
	// injected
	state?: RootState
}

@connect(state => ({ state }))
@injectRouter
class Breadcrumbs extends Component<BreadcrumbsProps, {}> {
	context: RouterContext;

	render() {
		const { state } = this.props;
		const { route } = state.router;
		if (!route) {
			return null;
		}

		return (
			<Breadcrumb>
				{_.keys(route._meta).map((item, index) => {
					const spec = _.get<RouteSpec>(segments, item);
					
					let name = spec.displayName;
					if (_.isFunction(name)) {
						name = (name as DisplayNameFunc)(route, state);
					}

					if (spec.abstract) {
						return <li key={index}>{name}</li>;
					}

					return (
						<Link key={index} 
							to={spec} params={route.params} 
							activeComponent="li">
							{name}
						</Link>
					)
				})}
			</Breadcrumb>
		)
	}
}

export default Breadcrumbs;
