import React = require('react');
import { Component } from 'react';

class Intro extends Component<{}, {}> {
	render() {
		return <div>
			<p>
				When Peach Fuzzer detects a fault, the fuzzer can collect
				and store additional information that can assist in performing root cause
				analysis of the fault.The logged information can include the following:
			</p>
			<ul>
				<li>Log files stored on disk or on remote machines via SSH</li>
				<li>Network traffic captures that can be loaded into Wireshark</li>
				<li>Serial console output from devices such as routers or switches</li>
			</ul>
			<p>
				The Pit Data Collection section is optional.
				Skip this section if this kind of data is not produced or
				you choose not to capture the data.
			</p>

			<div ng-switch="vm.Agents.length">
				<div ng-switch-when="0"></div>
				<div ng-switch-default>
					<h4>
						Items Already Added
					</h4>
					{this.renderTable()}
				</div>
			</div>
		</div>
	}

	renderTable() {
		const monitor = { description: 'foo' };
		return <table className="table table-striped table-hover table-bordered">
			<thead>
				<tr>
					<th className="peach-icon-cell"></th>
					<th>Data Collection Monitors</th>
				</tr>
			</thead>
			<tbody>
				<tr ng-repeat="agent in vm.Agents">
					<td className="peach-icon-cell center">
						<button className="btn btn-xs btn-danger"
							ng-click="vm.OnRemoveAgent($index)">
							<i className="fa fa-remove"></i>
						</button>
					</td>
					<td style={{ verticalAlign: 'middle' }}>
						<div ng-repeat="monitor in agent.monitors">
							{monitor.description}
						</div>
					</td>
				</tr>
			</tbody>
		</table>
	}
}

export default Intro;
