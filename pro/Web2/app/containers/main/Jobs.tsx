import React = require('react');
import { Component } from 'react';
import Icon = require('react-fa');

import JobsTable from '../../components/JobsTable';

class Jobs extends Component<{}, {}> {
	render() {
		return <div>
			<p>
				Here is a comprehensive list of the fuzzing jobs on this computer.
			</p>
			<p>
				For any entry, you can perform the following actions:
			</p>
			<ul>
				<li>
					Click the <Icon name='file-pdf-o' /> icon
					to view the report generated for the fuzzing session.
				</li>
				<li>
					Click the <Icon name='remove' /> icon
					to delete the job.
				</li>
			</ul>

			<JobsTable />
		</div>;
	}
}

export default Jobs;
