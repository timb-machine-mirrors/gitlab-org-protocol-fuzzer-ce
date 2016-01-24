import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'react-redux';

import { R } from '../../../routes';
import { Route } from '../../../models/Router';

interface MetricsProps extends Props<Metrics> {
	route?: Route;
}

@connect(state => ({ route: state.router.route }))
class Metrics extends Component<MetricsProps, {}> {
	render() {
		return <div>
			<p>
				This metric display presents statistics that are relevant for pits that 
				have state models with more than two or more states. 
				This display shows the number of times a specific state occurred during 
				the fuzzing session. 
				Seldom-used states might hide issues or indicate a problem.
			</p>
			<table
				st-table="vm.StateData"
				st-safe-src="vm.AllStateData"
				className="table table-striped table-bordered table-hover peach-table">
				<thead>
					<tr>
						<th st-sort="state"
								st-sort-default="true"
								className="width-100">
							State
						</th>
						<th st-sort="executionCount">
							Executions
						</th>
					</tr>
				</thead>
				<tbody>
					<tr>
						<td className="text-center"
								colSpan={2}
								ng-if="vm.AllStateData.length === 0">
							No data is available
						</td>
					</tr>
					<tr ng-repeat="row in vm.StateData">
						<td className="width-100">
							row.state
						</td>
						<td>
							row.executionCount
						</td>
					</tr>
				</tbody>
				<tfoot>
					<tr ng-if="vm.AllStateData.length > 25">
						<td colSpan={2}
								className="text-center">
							<div st-pagination
										st-items-by-page="25"
										st-displayed-pages="10">
							</div>
						</td>
					</tr>
				</tfoot>
			</table>
		</div>
	}
}

export default Metrics;
