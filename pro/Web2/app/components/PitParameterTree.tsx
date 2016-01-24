import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Panel } from 'react-bootstrap';
import { FieldProp } from 'redux-form';

import PitParameter from './PitParameter';
import FoldingPanel from './FoldingPanel';
import { Parameter, ParameterType } from '../models/Pit';

interface PitParameterTreeProps extends Props<PitParameterTree> {
	param: Parameter;
	fields: any;
	index: number;
}

class PitParameterTree extends Component<PitParameterTreeProps, {}> {
	render() {
		const { param, fields, index } = this.props;
		return this.renderTree(param, fields, index);
	}

	renderTree(param: Parameter, fields: any, index: number) {
		switch (param.type) {
			case ParameterType.Group:
				return this.renderGroup(param, fields.groups[index], index);
			case ParameterType.Space:
				return this.renderSpace(index);
			default:
				return this.renderLeaf(param, fields.params[index], index);
		}
	}

	renderGroup(group: Parameter, field: FieldProp, index: number) {
		return <div key={index} className="peach-parameter-group">
			<FoldingPanel defaultCollapsed={group.collapsed}
				header={isOpen => this.renderHeader(isOpen, group.name)}>
				{group.items.map((param, index) => 
					this.renderTree(param, field, index)
				)}
			</FoldingPanel>
		</div>
	}

	renderSpace(index: number) {
		return <hr key={index} />
	}

	renderLeaf(param: Parameter, field: FieldProp, index: number) {
		const options = {
			showLabel: true, 
			labelClassName: 'col-sm-4',
			wrapperClassName: 'col-sm-6'
		};
		return <PitParameter key={index}
			param={param} 
			field={field} 
			options={options} />
	}

	renderHeader(isOpen: boolean, name: string) {
		return (
			<span>
				<Icon name={isOpen ? 'chevron-down' : 'chevron-right'} />
				&nbsp;
				{name}
			</span>
		)
	}
}

export default PitParameterTree;
