import React = require('react');
import { ComponentClass, Component, Props, createElement } from 'react';
import { connect } from 'react-redux';

import { Route } from '../models/Router';
import { segments, RouteSpec } from '../routes';
import Error from '../containers/Error';
import { NotFound } from '../routes';

interface SegmentProps extends Props<Segment> {
	part: number;
	// injected
	route?: Route;
}

@connect(state => ({ route: state.router.route }))
class Segment extends Component<SegmentProps, {}> {
	render() {
		const { part, route } = this.props;
		if (!route) {
			return null;
		}

		const segment = _.get<RouteSpec>(segments, route.name);
		if (segment) {
			const component = _.get<ComponentClass<any>>(segment.parts, part);
			if (component)
				return createElement(component);
		}

		return this.renderNotFound();
	}

	renderNotFound() {
		const { part } = this.props;
		const component = _.get<ComponentClass<any>>(NotFound.parts, part);
		return createElement(component);
	}
}

export default Segment;
