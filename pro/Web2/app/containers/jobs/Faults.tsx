import React = require('react');
import { Component, Props } from 'react';
import { Row, Col } from 'react-bootstrap';

import FaultsTable from '../../components/FaultsTable';

class Faults extends Component<{}, {}> {
	render() {
		return <div>
			<p>
				For each session, the Faults view lists a summary of information about a fault such as:
			</p>

			<ul>
				<li>Identified fault iteration count</li>
				<li>Time and date</li>
				<li>Monitor that detected the fault</li>
				<li>Risk (if known) </li>
				<li>Bucket identifiers of the fault (major and minor hashes), if available</li>
			</ul>

			<Row>
				<Col xs={12}>
					<FaultsTable />
				</Col>
			</Row>
		</div>
	}
}

export default Faults;
