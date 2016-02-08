import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Badge, Table, ButtonGroup, Button } from 'react-bootstrap';
import Immutable = require('immutable');

interface Node {
	name: string;
	weight?: number;
	kids?: Node[];
}

interface FlatNode {
	name: string;
	weight: number;
	depth: number;
	visible: boolean;
	expanded: boolean;
}

interface TreeNodeProps extends Props<TreeNode> {
	node: FlatNode;
	onToggleExpand: Function;
}

interface TreeNodeState {
}

class TreeNode extends Component<TreeNodeProps, TreeNodeState> {
	render() {
		return this.renderRow();
	}

	renderRow() {
		const { node, onToggleExpand } = this.props;
		const { name, depth, weight, expanded, visible } = node;
		const fontWeight = 100 + weight * 100;
		const fontSize = this.getFontSize(weight);
		const expandIcon = expanded ? 'chevron-down' : 'chevron-right';
		const className = visible ? '' : 'collapse';

		return <tr className={className}>
			<td style={{ width: 80, textAlign: 'center', verticalAlign: 'middle' }}>
				&nbsp;
				<ButtonGroup bsSize='xs'>
					<Button>
						<Icon name='minus' />
					</Button>
					<Button>
						<Icon name='plus' />
					</Button>
					<Button>
						<Icon name='remove' />
					</Button>
				</ButtonGroup>
			</td>
			<td style={{ width: 50, textAlign: 'center', verticalAlign: 'middle' }}>
				<Badge>
					{weight ? weight : 0}
				</Badge>
			</td>
			<td>
				<span style={{ marginLeft: 10 + depth * 20 }}>
					<Button bsSize='xs' onClick={onToggleExpand}>
						<Icon name={expandIcon} />
					</Button>
					<label style={{ marginLeft: 5, fontWeight }}>
						{name}
					</label>
				</span>
			</td>
		</tr>
	}

	// renderNode() {
	// 	const { label, children, weight } = this.props;
	// 	// const fontWeight = weight ? 400 + (weight * 200) : 400;
	// 	const fontSize = this.getFontSize(weight);
	// 	const fontWeight = 'normal';
	// 	return <div className='tree-node'>
	// 		<Icon name='chevron-down' />
	// 		&nbsp;
	// 		<span className='tree-node-collapse-toggle' />
	// 		<span>
	// 			<input type='checkbox'
	// 				className='tree-node-checkbox'
	// 				checked={false} />
	// 			<label className='tree-node-label'>
	// 				{label}
	// 			</label>
	// 			<Badge>
	// 				{weight ? weight : 0}
	// 			</Badge>
	// 		</span>
	// 		<div className='tree-node-children'>
	// 			{children}
	// 		</div>
	// 	</div>
	// }

	getFontSize(weight: number) {
		switch (weight) {
			case 0:
				return 'xx-small';
			case 1:
				return 'x-small';
			case 2:
				return 'small';
			case 3:
			default:
				return 'medium';
			case 4:
				return 'large';
			case 5:
				return 'x-large';
			case 6:
				return 'xx-large';
		}
	}
}

interface TreeViewProps extends Props<TreeView> {
	data: Node[];
	// injected
}

interface TreeViewState {
	nodes: Immutable.List<FlatNode>;
}

class TreeView extends Component<TreeViewProps, TreeViewState> {
	constructor(props: TreeViewProps, context) {
		super(props, context);
		this.state = {
			nodes: Immutable.List(flatten(this.props.data, 0))
		}
	}
	
	render() {
		return this.renderTable();
	}

	// renderTree() {
	// 	const { children } = this.props;
	// 	return <div className='tree'>
	// 		{children}
	// 	</div>
	// }

	renderTable() {
		const { nodes } = this.state;
		return <Table condensed hover bordered>
			<thead>
				<th style={{ width: 80, textAlign: 'center' }}>
					Actions
				</th>
				<th style={{ width: 50, textAlign: 'center' }}>
					Weight
				</th>
				<th>
					Field
				</th>
			</thead>
			<tbody>
				{nodes.map((node, index) => {
					return <TreeNode key={index} 
						node={node} 
						onToggleExpand={() => this.onToggleExpand(index)} />
				})}
			</tbody>
		</Table>
	}

	onToggleExpand = (index: number) => {
		this.setState(({nodes}) => {
			const node = nodes.get(index);
			node.expanded = !node.expanded;
			let newNodes = nodes.set(index, node);
			nodes.skip(index + 1).forEach((next, key) => {
				if (next.depth <= node.depth) {
					return false;
				}
				next.visible = node.expanded;
				newNodes = newNodes.set(index + 1 + key, next);
			})
			return { nodes: newNodes };
		});
	}
}

function flatten(nodes: Node[], depth: number) {
	return _.flatMap(nodes, (node: Node) => {
		return [{
			name: node.name,
			weight: _.isUndefined(node.weight) ? 3 : node.weight,
			depth: depth,
			visible: true,
			expanded: true
		}].concat(flatten(node.kids, depth + 1));
	});
}

interface TuningProps extends Props<Tuning> {
	// injected
}

interface TuningState {
}

class Tuning extends Component<TuningProps, TuningState> {
	render() {
		const data = [
			{ name: 'S1_1', kids: [
				{ name: 'A3', kids: [
					{ name: 'TheDataModel', kids: [
						{ name: 'Header', weight: 6, kids: [
							{ name: 'Depth', weight: 6 }
						]},
						{ name: 'Data', kids: [
							{ name: 'Type', weight: 4 }
						]}
					]}
				]},
				{ name: 'A4', weight: 2, kids: [
					{ name: 'TheDataModel', kids: [
						{ name: 'Header', weight: 1, kids: [
							{ name: 'Depth', weight: 5 }
						]},
						{ name: 'Data', kids: [
							{ name: 'Type', weight: 4 }
						]}
					]}
				]}
			]},
			{ name: 'S4_1', kids: [
				{ name: 'Item1', weight: 1 },
				{ name: 'Item2', weight: 0 }
			]}
		];

		return <TreeView data={data} />
	}
}

export default Tuning;
