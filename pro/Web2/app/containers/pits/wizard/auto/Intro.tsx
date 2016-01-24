import React = require('react');
import { Component } from 'react';

class Intro extends Component<{}, {}> {
	render() {
		return <div>
			<p>
				The Pit Automation section allows you to set up actions that Peach will perform
				during fuzzing iterations to automate the test environment.
			</p>

			<p>Some sample automation actions include the following: </p>
			<ul>
				<li>Control the electrical power to a device</li>
				<li>Start or stop processes</li>
				<li>Run commands locally or remotely via SSH</li>
				<li>Click buttons, or simulate plugging and unplugging cables</li>
			</ul>

			<p>
				The Pit Automation section is optional.
				Skip this section if you don't have the appropriate hardware or
				you choose not to to automate any processes.
			</p>

			<p>
				If you want to automate more than one action, you will need to repeat
				the Pit Automation section process for each action you want to automate.
			</p>

			<p>
				The Pit Automation section requires the following information:
			</p>
			<ul>
				<li>
					Where the automated task runs: on a local machine or on a remote machine
				</li>
				<li>
					If the automation is on a remote machine, the operating system and the name
					(hostname or IP address) of the remote machine
				</li>
				<li>
					The process to automate (such as the CanaKit Relay board or a process running on the selected machine)
				</li>
				<li>
					Serial port, and related relay information (CanaKit Relay)
				</li>
				<li>
					Executable name and location, arguments and actions to take (process)
				</li>
			</ul>

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
					<th>Automation Monitors</th>
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
