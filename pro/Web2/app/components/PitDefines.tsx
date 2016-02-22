import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Button, Panel, Table } from 'react-bootstrap';
import { FieldProp } from 'redux-form';

import PitParameter from './PitParameter';
import FoldingPanel from './FoldingPanel';
import { Parameter } from '../models/Pit';

interface PitDefinesProps extends Props<PitDefines> {
	param: Parameter;
	fields: FieldProp[];
}

class PitDefines extends Component<PitDefinesProps, {}> {
	render() {
		const canRemove = false;
		const { param, fields } = this.props;
		const pairs = _.zip<any>(param.items, fields).map(item => ({
			param: item[0] as Parameter,
			field: item[1] as FieldProp
		}));

		return <FoldingPanel
			header={this.renderHeader}
			defaultCollapsed={param.collapsed}>
			<Table striped={true}
				hover={true}
				bordered={true}>
				<thead>
					<tr>
						<th className='nowrap'>Name</th>
						<th className='nowrap'>Key</th>
						<th className='width-100'>Value</th>
						{canRemove &&
							<th className='center nowrap'>Remove</th>
						}
					</tr>
				</thead>
				<tbody>
					{pairs.map((item, index) =>
						<tr key={index}>
							<td className='nowrap align-middle'>
								{item.param.name}
							</td>
							<td className='nowrap align-middle'>
								{item.param.key}
							</td>
							<td className='width-100'>
								<PitParameter param={item.param} field={item.field} />
							</td>
							{canRemove &&
								<td className='center nowrap align-middle'>
									<Button bsStyle='danger' bsSize='xs'
										onClick={() => this.onRemove(index)}>
										<Icon name='remove' />
									</Button>
								</td>
							}
						</tr>
					)}
				</tbody>
			</Table>
		</FoldingPanel>;
	}

	renderHeader = (isOpen: boolean) => {
		const { param } = this.props;
		return <span>
			<Icon name={isOpen ? 'chevron-down' : 'chevron-right'} />
			&nbsp; {param.name}
		</span>;
	};

	onRemove(index: number) {
	}
}

export default PitDefines;
