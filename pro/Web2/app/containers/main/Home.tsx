import React = require('react');
import { Component } from 'react';
import Icon = require('react-fa');

import JobsTable from '../../components/JobsTable';

class Home extends Component<{}, {}> {
	render() {
		return <div>
			<p>
				Welcome to the Peach Fuzzer &reg; UI home page,
				where you can do the following:
			</p>
			<ul>
				<li>
					Review results of fuzzing jobs, recent and not so recent
				</li>
				<li>
					Run new fuzzing jobs
				</li>
				<li>
					Replay fuzzing jobs to reproduce issues or to validate fixes of issues
				</li>
				<li>
					Review fuzzing definitions (pits) installed on your system
				</li>
				<li>
					Create and edit test configurations for the various pits
				</li>
			</ul>
			<p>
				To begin, click on the <Icon name="book" /> Library on the left and
				select a pit to configure, then run in a new fuzzing job.
			</p>

			<hr/>

			<h4>Recent Jobs</h4>

			<p>
				Here are the most recent fuzzing jobs.
			</p>
			<p>
				For any entry, you can perform the following actions:
			</p>
			<ul>
				<li>
					Click the <Icon name="file-pdf-o" /> icon
					to view the report generated for the fuzzing session.
				</li>
				<li>
					Click the <Icon name="remove" /> icon
					to delete the job.
				</li>
			</ul>

			<JobsTable limit={10} />
		</div>
	}
}

export default Home;
