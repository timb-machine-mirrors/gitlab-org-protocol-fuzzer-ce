import React = require('react');
import Icon = require('react-fa');
import { CSSProperties, Component, Props } from 'react';
import { Alert } from 'react-bootstrap';
import { connect } from 'redux-await';

import { Pit, PitFieldNode, PitWeight } from '../../../models/Pit';

const SHIFT_WIDTH = 20;

interface FlatNode {
	node: PitFieldNode;
	parent: FlatNode;
	fullId: string;
	depth: number;
	visible: boolean;
	expanded: boolean;
	display: string;
}

interface Result {
	nodes: FlatNode[];
	total: number;
}

function flatten(tree: PitFieldNode[]): Result {
	console.time('flatten');

	const result: Result = {
		nodes: [],
		total: 0
	};

	_flatten(tree, 0, '', null, result);

	console.timeEnd('flatten');

	return result;
}

function _flatten(
	nodes: PitFieldNode[],
	depth: number,
	prefix: string,
	parent: FlatNode,
	result: Result) {
	nodes.forEach(node => {
		const fullId = `${prefix}${node.id}`;
		const expanded = _.isUndefined(node.expanded) ?
			depth < 2 :
			node.expanded;
		const visible = !parent || parent.expanded && parent.visible;
		const flat: FlatNode = {
			node,
			parent,
			fullId,
			depth,
			visible,
			expanded,
			display: node.id
		};

		result.nodes.push(flat);
		result.total++;

		_flatten(node.fields, depth + 1, `${fullId}.`, flat, result);
	});
}

function defaultWeight(node: PitFieldNode) {
	return _.isUndefined(node.weight) ? 3 : node.weight;
}

function matchWeight(node: PitFieldNode, weight: number) {
	return (defaultWeight(node) === weight) ||
		_.some(node.fields, field => matchWeight(field, weight));
}

function cloneFields(fields: PitFieldNode[]): PitFieldNode[] {
	return fields.map(item => ({
		id: item.id,
		fields: cloneFields(item.fields)
	}));
}

function selectWeight(node: PitFieldNode, weight: number) {
	node.weight = weight;
	const fields = node.fields || [];
	fields.forEach(field => selectWeight(field, weight));
}

function applyWeights(weights: PitWeight[], fields: PitFieldNode[]) {
	console.time('applyWeights');
	for (const rule of weights) {
		const parts = rule.id.split('.');
		applyWeight(fields, parts, rule.weight);
	}
	console.timeEnd('applyWeights');
}

function applyWeight(fields: PitFieldNode[], parts: string[], weight: number) {
	const next = parts.shift();
	for (const node of fields) {
		if (node.id === next) {
			if (parts.length === 0) {
				node.weight = weight;
			} else {
				applyWeight(node.fields, parts, weight);
			}
		}
	}
}

function extractWeights(prefix: string, tree: PitFieldNode[], collect: PitWeight[]) {
	for (const node of tree) {
		const here = `${prefix}${node.id}`;
		if (defaultWeight(node) !== 3) {
			collect.push({ id: here, weight: node.weight });
		}
		extractWeights(`${here}.`, node.fields, collect);
	}
}

interface TuningProps extends Props<Tuning> {
	// injected
	pit?: Pit;
}

interface TuningState {
	tree?: PitFieldNode[];
	nodes?: FlatNode[];
}

@connect(state => ({ pit: state.pit }))
class Tuning extends Component<TuningProps, TuningState> {
	constructor(props, context) {
		super(props, context);
		this.state = {
			tree: null,
			nodes: []
		};

		setTimeout(() => {
			const { pit } = this.props;
			console.time('load');
			const tree = cloneFields(pit.metadata.fields);
			applyWeights(pit.weights, tree);

			const result = flatten(tree);
			this.setState({ tree, nodes: result.nodes });
			console.timeEnd('load');
			console.log('nodes', result.total);
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
			<div className='tuning'>
				<div>
					<table>
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
		const textStyle: CSSProperties = {
		};
		return <table>
			<tbody>
				<tr>
					<td>
						{_.range(7).map(i => <div key={i} className='tuning-legend-text'>
							{_.range(0, i).map(j => (
								<span key={j} className={(j === 0) ? 'line first' : 'line'} />
							))}
							<span className='text' style={textStyle}>
								{texts[i]}
							</span>
						</div>)}
						<div className='tuning-legend'>
							<span>
								{_.range(6).map(i => <Icon key={i}
									className={(i === 0) ? 'radio first' : 'radio'}
									name='circle-thin'
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
					'circle-thin'
		);
		return <th>
			{_.range(6).map(i => <Icon key={i}
				className={(i === 0) ? 'radio first' : 'radio'}
				name={icons[i]}
				size='lg'
				onClick={() => this.onSelectWeight(node, i)}
			/>)}
		</th>;
	}

	renderRowCell(node: FlatNode) {
		const nodeStyle: CSSProperties = {
			marginLeft: node.depth * SHIFT_WIDTH,
			cursor: _.isEmpty(node.node.fields) ? '' : 'pointer'
		};
		const expanderIcon = node.expanded ? 'minus' : 'plus';

		return <td>
			<div className='node'
				style={nodeStyle}
				onClick={() => this.onToggleExpand(node)}>
				<span className='expander'>
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
		const { nodes } = flatten(tree);
		this.setState({ nodes });
	};

	onSelectWeight = (node: FlatNode, weight: number) => {
		const { tree } = this.state;
		selectWeight(node.node, weight);
		const { nodes } = flatten(tree);
		this.setState({ nodes });
	};
}

export default Tuning;
