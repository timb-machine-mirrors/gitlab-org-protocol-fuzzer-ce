import React = require('react');
import { Component, Props } from 'react';
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { Row, Col, Table } from 'react-bootstrap';

import { R } from '../../routes';
import { FaultDetail } from '../../models/Fault';
import { formatDate, formatFileSize } from '../../utils';
import { fetchFault } from '../../redux/modules/Fault';
import { Route } from '../../models/Router';

const NullFault: FaultDetail = {
	id: null,
	faultUrl: null,
	archiveUrl: null,
	reproducible: null,
	iteration: null,
	timeStamp: null,
	source: null,
	exploitability: null,
	majorHash: null,
	minorHash: null,
	pitUrl: null,
	title: null,
	description: null,
	seed: null,
	files: [],
	iterationStart: null,
	iterationStop: null
};

interface FaultsDetailProps extends Props<FaultsDetail> {
	// injected
	fault?: FaultDetail;
	route?: Route;
	dispatch?: Dispatch;
	statuses?: any;
}

@connect(state => ({ 
	fault: state.fault,
	route: state.router.route
}))
class FaultsDetail extends Component<FaultsDetailProps, {}> {
	componentDidMount(): void {
		const { route } = this.props;
		this.props.dispatch(fetchFault(route.params));
	}
	
	render() {
		const { fault, statuses } = this.props;
		const data = (fault && statuses.fault === 'success') ? fault : NullFault;
		return <Row>
			<Col xs={12}>
				<Table className="peach-fault-detail">
					<tbody>
						<tr>
							<td>Test Case</td>
							<td>{data.iteration}</td>
						</tr>
						{data.iterationStop !== data.iterationStop &&
							<tr>
								<td>Iteration Range</td>
								<td>{data.iterationStart} - {data.iterationStop}</td>
							</tr>
						}
						<tr>
							<td>Reproducible</td>
							<td>{data.reproducible ? 'Yes' : 'No'}</td>
						</tr>
						<tr>
							<td>Title</td>
							<td>{data.title}</td>
						</tr>
						<tr>
							<td>When</td>
							<td>{formatDate(data.timeStamp) }</td>
						</tr>
						<tr>
							<td>Source</td>
							<td>{data.source}</td>
						</tr>
						<tr>
							<td>Risk</td>
							<td>{data.exploitability}</td>
						</tr>
						<tr>
							<td>Major Hash</td>
							<td>{data.majorHash}</td>
						</tr>
						<tr>
							<td>Minor Hash</td>
							<td>{data.minorHash}</td>
						</tr>
						<tr>
							<td className="align-top">Description</td>
							<td>
								<pre>{data.description}</pre>
							</td>
						</tr>
						<tr>
							<td className="align-top">Generated Files</td>
							<td>
								{this.renderFilesTable(data) }
							</td>
						</tr>
					</tbody>
				</Table>
			</Col>
		</Row>
	}

	renderFilesTable(fault: FaultDetail) {
		return <Table hover={true} bordered={true}>
			<thead>
				<tr>
					<th st-ratio="85">
						Name
					</th>
					<th st-ratio="15"
						className="align-right">
						Size
					</th>
				</tr>
			</thead>
			<tbody>
				<tr>
					<td>
						<a href={fault.archiveUrl}>
							Full Archive
						</a>
					</td>
				<td></td>
				</tr>
				{fault.files.map((file, index) => (
					<tr key={index}>
						<td>
							<a href={file.fileUrl}>
								{file.fullName}
							</a>
						</td>
						<td className="align-right">
							{formatFileSize(file.size, 2)}
						</td>
					</tr>
				))}
			</tbody>
		</Table>
	}
}

export default FaultsDetail;
