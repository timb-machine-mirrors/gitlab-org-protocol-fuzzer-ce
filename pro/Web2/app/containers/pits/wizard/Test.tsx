import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Button, ButtonToolbar, Label } from 'react-bootstrap';
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
		const tracks = [
			{ name: 'foo', msg: 'msg1', link: 'link' },
			{ name: 'bar', msg: 'msg2', link: 'link' }
		];
		return <div>
			<p>
				When all of the sections of the configuration are complete,
				you can validate the fuzzing definition for correctness.
			</p>
			<p>
				This section performs a test that runs a single iteration of the Pit,
				outputs the results of the test, and issues log messages as appropriate.
				You can fine tune a configuration or address any problems that arise with
				a configuration by re-visiting the appropriate section of the configuration,
				adjusting the settings that you want to edit, and re-run the test.
			</p>

			<dl className="dl-horizontal">
				{tracks.map((item, index) => (
					<div key={index}>
						<dt>
							{item.name}
						</dt>
						<dd>
							<Label bsStyle='success'>
								{item.msg}
								&nbsp;
								<a ui-sref="{{item.start}}({track: item.id})"
									style={{ color: 'white' }}>
									<b>{item.link}</b>
								</a>
							</Label>
						</dd>
					</div>
				))}
			</dl>

			<div className="wizard-actions">
				<ButtonToolbar>
					<Button bsStyle="success" bsSize="sm"
						onClick={this.onContinue}
						ng-disabled="!vm.CanContinue">
						Continue &nbsp; <Icon name="arrow-right" />
					</Button>
					<Button bsStyle="warning" bsSize="sm"
						onClick={this.onBeginTest}
						disabled={!pit.isConfigured}>
						<Icon name="bolt" /> &nbsp; Begin Test
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
