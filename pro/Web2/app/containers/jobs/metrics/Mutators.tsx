import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'react-redux';

import { R } from '../../../containers';
import { Route } from '../../../models/Router';

interface MetricsProps extends Props<Metrics> {
	route?: Route;
}

@connect(state => ({ route: state.router.route }))
class Metrics extends Component<MetricsProps, {}> {
	render() {
		return <div>
			<p>
				This metric display shows statistics for each mutator.
			</p>
			<table
				st-table="vm.MutatorData"
				st-safe-src="vm.AllMutatorData"
				className="table table-striped table-bordered table-hover peach-table">
				<thead>
					<tr>
						<th st-sort="mutator"
							className="width-100">
							Mutator
						</th>
						<th st-sort="elementCount">
							Elements
						</th>
						<th st-sort="iterationCount">
							Test Cases
						</th>
						<th st-sort="bucketCount">
							Buckets
						</th>
						<th st-sort="faultCount"
							st-sort-default="reverse">
							Faults
						</th>
					</tr>
				</thead>
				<tbody>
					<tr>
						<td className="text-center"
								colSpan={5}
								ng-if="vm.AllMutatorData.length === 0">
							No data is available
						</td>
					</tr>
					<tr ng-repeat="row in vm.MutatorData">
						<td className="width-100">
							row.mutator
						</td>
						<td>
							row.elementCount
						</td>
						<td>
							row.iterationCount
						</td>
						<td>
							row.bucketCount
						</td>
						<td>
							row.faultCount
						</td>
					</tr>
				</tbody>
				<tfoot>
					<tr ng-if="vm.AllMutatorData.length > 25">
						<td colSpan={5}
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
