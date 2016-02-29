import React = require('react');
import Icon = require('react-fa');
import { CSSProperties, Component, Props } from 'react';
import { Alert } from 'react-bootstrap';
import { connect } from 'redux-await';

import { Pit, PitFieldNode } from '../../../models/Pit';

const SHIFT_WIDTH = 20;

interface FlatNode {
	node: PitFieldNode;
	depth: number;
	visible: boolean;
	expanded: boolean;
}

function flatten(tree: PitFieldNode[]): FlatNode[] {
	console.time('flatten');
	const flat = _flatten(tree, 0, null);
	console.timeEnd('flatten');
	return flat;
}

function _flatten(nodes: PitFieldNode[], depth: number, parent: FlatNode) {
	return _.flatMap(nodes, (node: PitFieldNode) => {
		const expanded = _.isUndefined(node.expanded) ?
			depth < 2 :
			node.expanded;
		const visible = !parent || parent.expanded && parent.visible;
		const flat: FlatNode = {
			node,
			depth,
			visible,
			expanded
		};
		return [flat].concat(_flatten(node.fields, depth + 1, flat));
	});
}

function defaultWeight(node: PitFieldNode) {
	return _.isUndefined(node.weight) ? 3 : node.weight;
}

function matchWeight(node: PitFieldNode, weight: number) {
	return (defaultWeight(node) === weight) ||
		_.some(node.fields, field => matchWeight(field, weight));
}

function selectWeight(node: PitFieldNode, weight: number) {
	node.weight = weight;
	const fields = node.fields || [];
	fields.forEach(field => selectWeight(field, weight));
}

const outerStyle: CSSProperties = {
	position: 'relative',
};

const innerStyle: CSSProperties = {
	overflowX: 'auto',
	overflowY: 'visible',
	marginLeft: 140
};

const headerStyle: CSSProperties = {
	position: 'absolute',
	left: 0,
	padding: '10px 0px 10px 10px'
};

const iconStyle: CSSProperties = {
	cursor: 'pointer',
	padding: 2
};

const iconStyleFirst: CSSProperties = Object.assign({}, iconStyle, {
	paddingRight: 10
});

const cellStyle: CSSProperties = {
	padding: '2px 2px 2px 35px'
};

const spanStyle: CSSProperties = {
	float: 'left',
	cursor: 'pointer',
	marginLeft: -35
};

const nodeGenericStyle: CSSProperties = {
	border: '1px solid black',
	borderRadius: '3px',
	padding: 7,
	marginRight: 5,
	whiteSpace: 'nowrap'
};

interface TuningProps extends Props<Tuning> {
	// injected
	tree?: PitFieldNode[];
}

interface TuningState {
	tree?: PitFieldNode[];
	nodes?: FlatNode[];
}

@connect(state => ({ tree: state.pit.metadata.fields }))
class Tuning extends Component<TuningProps, TuningState> {
	constructor(props, context) {
		super(props, context);
		this.state = {
			tree: null,
			nodes: []
		};

		setTimeout(() => {
			console.time('load');
			const tree = _.cloneDeep(props.tree);
			const nodes = flatten(tree);
			this.setState({ tree, nodes });
			console.timeEnd('load');
			console.log('nodes', nodes.length);
		});
	}

	render() {
		const { tree, nodes } = this.state;
		console.time('render');
		const ret = <div>
			{this.renderLegend()}
			<hr />
			{_.isNull(tree) &&
				<Alert bsStyle='info'>
					Loading data...
				</Alert>
			}
			<div style={outerStyle}>
				<div style={innerStyle}>
					<table style={{ width: '100%' }}>
						<tbody>
							{nodes.map((node, index) => node.visible &&
								this.renderNode(node, index)
							)}
						</tbody>
					</table>
				</div>
			</div>
		</div>;
		console.timeEnd('render');
		return ret;
	}

	renderLegend() {
		const texts = [
			'Exclude',
			'Lowest',
			'Low',
			'Normal',
			'High',
			'Highest'
		];
		const divStyle: CSSProperties = {
			lineHeight: 1.2
		};
		const spanStyle: CSSProperties = {
			padding: 10
		};
		const textStyle: CSSProperties = {
			paddingLeft: 9
		};
		const lineStyle: CSSProperties = {
			border: '1px solid black',
			borderWidth: '0 0 0 1px'
		};
		const spaceStyle = (i: number): CSSProperties => (
			(i === 0) ? {
				paddingLeft: 19,
				paddingRight: 8
			} : {
				paddingLeft: 19
			}
		);
		return <table>
			<tbody>
				<tr>
					<td>
						{_.range(7).map(i => <div style={divStyle}>
							{_.range(0, i).map(j => (
								<span style={spaceStyle(j)}>
									<span style={lineStyle} />
								</span>
							))}
							<span style={textStyle}>{texts[i]}</span>
						</div>)}
						<div>
							<span style={spanStyle}>
								{_.range(6).map(i => <Icon key={i}
									style={(i === 0) ? iconStyleFirst : iconStyle}
									name='circle-o'
									size='lg'
								/>)}
							</span>
						</div>
					</td>
					<td>
						{this.renderLegendDescription()}
					</td>
				</tr>
			</tbody>
		</table>;
	}

	renderLegendDescription() {
		return <p>
			A bunch of text goes here.
		</p>;
	}

	renderNode(node: FlatNode, key: number) {
		return <tr key={key}>
			{this.renderRowHeader(node)}
			{this.renderRowCell(node)}
		</tr>;
	}

	renderRowHeader(node: FlatNode) {
		const weight = defaultWeight(node.node);
		const icons = _.range(6).map(i =>
			(weight === i) ? 'circle' :
				(!node.expanded && matchWeight(node.node, i)) ?
					'dot-circle-o' :
					'circle-o'
		);
		return <th style={headerStyle}>
			{_.range(6).map(i => <Icon key={i}
				style={(i === 0) ? iconStyleFirst : iconStyle}
				name={icons[i]}
				size='lg'
				onClick={() => this.onSelectWeight(node, i)}
			/>)}
		</th>;
	}

	renderRowCell(node: FlatNode) {
		const nodeStyle = Object.assign({}, nodeGenericStyle, {
			marginLeft: node.depth * SHIFT_WIDTH,
		});
		const expanderIcon = node.expanded ? 'minus' : 'plus';

		return <td style={cellStyle}>
			<div style={nodeStyle}>
				<span style={spanStyle} onClick={() => this.onToggleExpand(node)}>
					{!_.isEmpty(node.node.fields) &&
						<Icon name={expanderIcon} />
					}
				</span>
				{node.node.id}
			</div>
		</td>;
	}

	onToggleExpand = (node: FlatNode) => {
		const { tree } = this.state;
		node.node.expanded = !node.expanded;
		this.setState({
			nodes: flatten(tree)
		});
	};

	onSelectWeight = (node: FlatNode, weight: number) => {
		const { tree } = this.state;
		selectWeight(node.node, weight);
		this.setState({
			nodes: flatten(tree)
		});
	};
}

export default Tuning;
