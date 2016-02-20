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
		return (
			<div>
				<p>
					This metric display shows statistics for all of the elements in your Pit.
				</p>
				<table
					st-table="vm.ElementData"
					st-safe-src="vm.AllElementData"
					className="table table-striped table-bordered table-hover peach-table">
					<thead>
						<tr>
							<th st-sort="state">
								State
							</th>
							<th st-sort="action">
								Action
							</th>
							<th st-sort="element"
								className="width-100">
								Element
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
									colSpan={7}
									ng-if="vm.AllElementData.length === 0">
								No data is available
							</td>
						</tr>
						<tr ng-repeat="row in vm.ElementData">
							<td className="max-width-200 break-word">
								row.state
							</td>
							<td className="max-width-200 break-word">
								row.action
							</td>
							<td className="max-width-500 width-100 break-word">
								row.element
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
						<tr ng-if="vm.AllElementData.length > 25">
							<td colSpan={7}
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
		)
	}
}

export default Metrics;
