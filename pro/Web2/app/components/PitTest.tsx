import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { connect } from 'redux-await';
import { Tabs, Tab, Row, Col, Alert, Table } from 'react-bootstrap';

import { TestState, TestEvent, TestStatus } from '../models/PitTest';

interface PitTestProps extends Props<PitTest> {
	// injected
	test?: TestState;
}

@connect(state => ({ test: state.test }))
class PitTest extends Component<PitTestProps, {}> {
	render() {
		const { test } = this.props;

		return (
			<Row>
				<hr />

				<Col md={12}>
					{test.isPending &&
						<Alert bsStyle='info'>
							Testing is in progress.
						</Alert>
					}

					{test.result && test.result.status === TestStatus.Pass &&
						<Alert bsStyle='success'>
							Testing passed, click Continue.
						</Alert>
					}

					{test.result && test.result.status === TestStatus.Fail &&
						<Alert bsStyle='danger'>
							Testing failed, please correct the errors described and 
							return to this page to test again.
						</Alert>
					}

					<h3>
						Test Output
					</h3>
					<Tabs defaultActiveKey={1}>
						{this.renderSummary()}
						{this.renderLog()}
					</Tabs>
				</Col>
			</Row>
		)
	}

	renderSummary() {
		const { test } = this.props;
		const events = test.result ? test.result.events : [];
		const status = test.result ? test.result.status : TestStatus.Fail;
		return <Tab eventKey={1} title="Summary">
			<Table striped hover bordered>
				<thead>
					<tr>
						<th>
							{this.renderStatusIcon(status)}
						</th>
						<th style={{ width: '100%' }}>
							Message
						</th>
						</tr>
					</thead>
				<tbody>
					{!test.isPending && _.isEmpty(events) &&
						<tr>
							<td style={{ textAlign: 'center' }}
								colSpan={2}>
								Test has not been started.
							</td>
						</tr>
					}
					{test.isPending && _.isEmpty(events) &&
						<tr>
							<td style={{ textAlign: 'center' }}
								colSpan={2}>
								Waiting for test events...
							</td>
						</tr>
					}
					{events.map(this.renderTestEvent) }
				</tbody>
			</Table>
		</Tab>
	}

	renderLog() {
		const { test } = this.props;
		const log = test.result ? test.result.log : '';
		return <Tab eventKey={2} title="Log">
			<pre className="peach-test-log">
				{log}
			</pre>
		</Tab>
	}

	renderTestEvent = (item: TestEvent, index: number) => {
		return <tr key={index}>
			<td>
				{this.renderStatusIcon(item.status)}
			</td>
			<td style={{ width: '100%' }}>
				<span>
					{item.description}
				</span>
				{item.resolve &&
					<span className="red">
						<br />
						{item.resolve}
					</span>
				}
			</td>
		</tr>
	}

	renderStatusIcon(status: string) {
		switch (status) {
			case TestStatus.Pass:
				return <Icon name='check' className='green' />
			case TestStatus.Fail:
				return <Icon name='ban' className='red' />
			default:
				return <Icon name='spinner' pulse />
		}
	}
}

export default PitTest;
