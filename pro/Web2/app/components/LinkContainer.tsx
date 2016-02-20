import React = require('react');
import { Component, Props, Children, ReactElement, cloneElement } from 'react';
import { connect } from 'react-redux';
import { actions } from 'redux-router5';

import { BaseLinkProps, createLinkDescriptor } from './Link';
import { injectRouter, RouterContext } from '../models/Router';

interface LinkContainerProps extends BaseLinkProps, Props<LinkContainer> {
}

@connect(() => ({}))
@injectRouter
class LinkContainer extends Component<LinkContainerProps, {}> {
	context: RouterContext;

	render() {
		const child = Children.only(this.props.children) as ReactElement<any>;
		const props = createLinkDescriptor(this.context.router, this.props);
		return cloneElement(child, props);
	}
}

export default LinkContainer;
