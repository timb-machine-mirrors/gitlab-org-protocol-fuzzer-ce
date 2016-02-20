import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Dispatch } from 'redux';
import { Alert, Button, ButtonToolbar } from 'react-bootstrap';
import { connect } from 'react-redux';
import { actions } from 'redux-router5';

import { R } from '../../../containers';
import PitTest from '../../../components/PitTest';
import { Pit } from '../../../models/Pit';
import { TestState, TestStatus } from '../../../models/PitTest';
import { startTest, stopTest } from '../../../redux/modules/PitTest';

interface TestProps extends Props<Test> {
	// injected
	pit?: Pit;
	test?: TestState;
	dispatch?: Dispatch;
}

@connect(state => ({ 
	pit: state.pit,
	test: state.test
}))
class Test extends Component<TestProps, {}> {
	render() {
		const { pit, test } = this.props;
		const canContinue = !test.isPending && 
			test.result && test.result.status === TestStatus.Pass;

		return <div>
			<p>
				This section validates the configuration by executing one test case,
				and exposes issues that would hinder a fuzzing session.The output will
				display any warnings and errors that surface in the control test case.
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
						disabled={!canContinue}>
						Continue &nbsp; <Icon name='arrow-right' />
					</Button>
					{!test.isPending &&
						<Button bsStyle='warning' bsSize='sm'
							onClick={this.onBeginTest}
							disabled={!pit.isConfigured}>
							<Icon name='bolt' /> &nbsp; Begin Test
						</Button>
					}
					{test.isPending &&
						<Button bsStyle='danger' bsSize='sm'
							onClick={this.onAbortTest}>
							<Icon name='stop' /> &nbsp; Abort Test
						</Button>
					}
				</ButtonToolbar>
			</div>

			<PitTest />
		</div>
	}

	onContinue = () => {
		const { dispatch, pit } = this.props;
		const to = R.Pit.name;
		const params = { pit: pit.id };
		dispatch(actions.navigateTo(to, params));
	}

	onBeginTest = () => {
		const { pit, dispatch } = this.props;
		dispatch(startTest(pit));
	}

	onAbortTest = () => {
		const { test, dispatch } = this.props;
		dispatch(stopTest(test.job));
	}
}

export default Test;
