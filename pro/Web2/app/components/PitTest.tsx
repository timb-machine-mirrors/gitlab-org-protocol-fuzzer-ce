import React = require('react');
import { Component } from 'react';
import { Tabs, Tab } from 'react-bootstrap';

class PitTest extends Component<{}, {}> {
	render() {
		const testTime = 0;
		const testLog = 'nothing here';
		const testEvents = [
			{ description: 'event1', resolve: 'resolve1' }
		];
		return (
			<div className="row"
				ng-if="vm.IsAvailable">

				<hr />

				<div className="col-md-12">
					<div className="alert alert-info"
						ng-show="vm.ShowTestPending">
						Testing is in progress.
					</div>

					<div className="alert alert-success"
						ng-show="vm.ShowTestPass">
						Testing passed, click Continue.
					</div>

					<div className="alert alert-danger"
						ng-show="vm.ShowTestFail">
						Testing failed, please correct the errors described and return to this page to test again.
					</div>

					<h3>
						Test Output
						<span ng-switch="vm.TestTime.length">
							<span ng-switch-when="0" />
							<span ng-switch-default>({testTime}) </span>
						</span>
					</h3>
					<Tabs defaultActiveKey={1}>
						<Tab title="Summary"
							eventKey={1}>
							<table className="table table-striped table-hover table-bordered">
								<thead>
									<tr>
										<th><i ng-className="vm.StatusClass(null)"></i></th>
										<th className="width-100">Message</th>
									</tr>
								</thead>
								<tbody>
									<tr>
										<td className="text-center"
											colSpan={2}
											ng-if="vm.TestEvents.length === 0">
											Waiting for test events...
										</td>
									</tr>
									{testEvents.map((item, index) => {
										<tr>
											<td>
												<i ng-className="vm.StatusClass(row)"></i>
											</td>
											<td className="width-100">
												<span>
													{item.description}
												</span>
												<span className="red"
													ng-show="row.resolve">
													<br />
													{item.resolve}
												</span>
											</td>
										</tr>
									})}
								</tbody>
							</table>
						</Tab>
						<Tab title="Log"
							eventKey={2}>
							<pre className="peach-test-log">{testLog}</pre>
						</Tab>
					</Tabs>
				</div>
			</div>
		)
	}
}

export default PitTest;
