import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'react-redux';

import { DisplayNameFunc, RouteSpec, segments } from '../routes';
import RootState from '../models/Root';

interface BreadcrumbLeafProps extends Props<BreadcrumbLeaf> {
	// injected
	state?: RootState
}

@connect(state => ({ state }))
class BreadcrumbLeaf extends Component<BreadcrumbLeafProps, {}> {
	render() {
		const { state } = this.props;
		const { route } = state.router;
		if (!route) {
			return null;
		}

		const segment = _.get<RouteSpec>(segments, route.name);
		let name = segment.displayName;
		if (_.isFunction(name)) {
			name = (name as DisplayNameFunc)(route, state);
		}
		return <h1>{ name }</h1>;
	}
}

export default BreadcrumbLeaf;
