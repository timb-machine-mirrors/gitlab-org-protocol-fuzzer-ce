import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'redux-await';
import { Dispatch } from 'redux';

import { Route } from '../../models/Router';
import Segment from '../../components/Segment';
import { fetchPit } from '../../redux/modules/Pit';

interface PitProps extends Props<Pit> {
	// injected
	route?: Route;
	dispatch?: Dispatch;
}

@connect(state => ({ route: state.router.route }))
class Pit extends Component<PitProps, {}> {
	componentDidMount() {
		this.load(this.props);
	}

	load(props: PitProps) {
		const { dispatch, route } = props;
		const { pit } = route.params;
		dispatch(fetchPit(pit));
	}

	render() {
		return <Segment part={2} />;
	}
}

export default Pit;
