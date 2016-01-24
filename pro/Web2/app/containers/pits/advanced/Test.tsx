import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Alert, Button, ButtonToolbar } from 'react-bootstrap';
import { connect } from 'react-redux';

import PitTest from '../../../components/PitTest';
import { Pit } from '../../../models/Pit';

interface TestProps extends Props<Test> {
	// injected
	pit?: Pit;
}

@connect(state => ({ pit: state.pit }))
class Test extends Component<TestProps, {}> {
	render() {
		const { pit } = this.props;
		return <div>
			<p>
				This section validates the configuration by executing one test iteration,
				and exposes issues that would hinder a fuzzing session.The output will
				display any warnings and errors that surface in the control iteration.
				Detailed log messages are provided to help diagnose issues with the
				configuration.
			</p>
			<p>
				Click the Begin Test button to validate the configuration.
			</p>

			{!pit.isConfigured &&
				<Alert bsStyle='danger'>
					<strong>Error!</strong>
					&nbsp;
					The current Pit has required configuration variables that must be set.
				</Alert>
			}

			{pit.isConfigured && !pit.hasMonitors &&
				<Alert bsStyle='warning'>
					<strong>Warning!</strong>
					&nbsp;
					The current Pit is not configured to monitor the environment.
				</Alert>
			}

			<div className="wizard-actions">
				<ButtonToolbar>
					<Button bsStyle='success' bsSize='sm'
						onClick={this.onContinue}
						ng-disabled="!vm.CanContinue">
						Continue &nbsp; <Icon name='arrow-right' />
					</Button>
					<Button bsStyle='warning' bsSize='sm'
						onClick={this.onBeginTest}
						disabled={!pit.isConfigured}>
						<Icon name='bolt' /> &nbsp; Begin Test
					</Button>
				</ButtonToolbar>
			</div>

			<PitTest />
		</div>
	}

	onContinue = () => {
	}

	onBeginTest = () => {
	}
}

export default Test;
