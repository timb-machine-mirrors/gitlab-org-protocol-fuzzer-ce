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
		return (
			<div>
				<p>
					This metric display shows statistics related to the use of two or more data sets 
					in the fuzzing session. 
					This is useful to determine the origin of unique buckets and also faults in terms 
					of the data sources used in mutating.
				</p>

				<table
					st-table="vm.DatasetData"
					st-safe-src="vm.AllDatasetData"
					className="table table-striped table-bordered table-hover peach-table">
					<thead>
						<tr>
							<th st-sort="dataset"
								className="width-100">
								Data Set
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
									colSpan={4}
									ng-if="vm.AllDatasetData.length === 0">
								No data is available
							</td>
						</tr>
						<tr ng-repeat="row in vm.DatasetData">
							<td className="max-width-500 width-100 break-word">
								dataset
							</td>
							<td>
								iterationCount
							</td>
							<td>
								bucketCount
							</td>
							<td>
								faultCount
							</td>
						</tr>
					</tbody>
					<tfoot>
						<tr ng-if="vm.AllDatasetData.length > 25">
							<td colSpan={4}
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
