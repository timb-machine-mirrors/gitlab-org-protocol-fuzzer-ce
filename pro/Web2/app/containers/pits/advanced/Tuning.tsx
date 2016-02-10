import React = require('react');
import Icon = require('react-fa');
import { CSSProperties, Component, Props } from 'react';
import { Badge, Table, ButtonGroup, Button, Input } from 'react-bootstrap';
import Immutable = require('immutable');
import Tree = require('rc-tree');

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

interface SelectProps extends Props<Select> {
	value: string;
	color: string;
}

class Select extends Component<any, any> {
	render() {
		const { value, color } = this.props;
		const style: CSSProperties = {
			position: 'relative',
			borderRadius: 4,
			border: '1px solid #ccc',
			cursor: 'pointer',
			backgroundColor: color
		};
		const picker: CSSProperties = {
			position: 'absolute',
			right: 0,
			top: 0,
			width: '1.9em',
			lineHeight: '2em'
		};
		const input: CSSProperties = {
			paddingTop: 0,
			paddingBottom: 0,
			paddingRight: 0,
			paddingLeft: 10,
			textAlign: 'left',
			lineHeight: '2em',
			fontWeight: 'bold'
		};
		return <div style={style}>
			<span style={picker}>
				<Icon name='caret-down' />
			</span>
			<div style={input}>
				{value}
			</div>
		</div>
	}
}

class Weight extends Component<any, any> {
	render() {
		const { item } = this.props;
		return <span style={{ backgroundColor: item.color }}>
			{item.name}
		</span>
	}
}

class TreeNode extends Component<TreeNodeProps, TreeNodeState> {
	render() {
		const { node, onToggleExpand } = this.props;
		const { name, depth, weight, expanded, visible } = node;
		const expandIcon = expanded ? 'minus' : 'plus';
		const className = visible ? '' : 'collapse';
		const weights = [
			{ value: 0, label: 'Exclude' },
			{ value: 1, label: 'Lowest', color: 'green' },
			{ value: 2, label: 'Low' },
			{ value: 3, label: 'Normal', color: 'yellow' },
			{ value: 4, label: 'High' },
			{ value: 5, label: 'Highest', color: 'red' }
		];
		const colors = [
			'#ffdf80',
			'#ebfaeb',
			'#c2f0c2',
			'#99e699',
			'#70db70',
			'#47d147',
		];

		return <tr className={className}>
			<td style={{ width: 100, textAlign: 'center' }}>
				<Select value={weights[weight].label} color={colors[weight]} />
			</td>
			<td style={{ verticalAlign: 'middle', position: 'relative' }}>
				<div style={{ paddingLeft: 10 + depth * 20, paddingRight: 50, whiteSpace: 'nowrap' }}>
					<Button bsSize='xs' onClick={onToggleExpand}>
						<Icon name={expandIcon} />
					</Button>
					<label style={{ marginLeft: 5, fontWeight: 'normal' }}>
						{name}
					</label>
				</div>
			</td>
		</tr>
	}

	renderValue(option) {
		return <strong style={{ color: option.color }}>{option.label}</strong>;
	}

	renderLine(depth, offset = 0) {
		const style: CSSProperties = {
			left: offset + 19,
			content: '',
			display: 'block',
			position: 'absolute',
			top: 0,
			bottom: 0,
			border: '1px dotted #9dbdd6',
			borderWidth: '0 0 0 1px'
		};
		
		if (depth == 0) {
			return null;
		}
		return <div style={style}>
			{this.renderLine(depth - 1)}
		</div>
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
		const { nodes } = this.state;
		return <div style={{ overflow: 'scroll' }}>
			<table>
				<thead>
					<tr>
						<th style={{ width: 100, textAlign: 'center' }}>
							Weight
						</th>
						<th>
							Field
						</th>
					</tr>
				</thead>
				<tbody>
					{nodes.map((node, index) => {
						return <TreeNode key={index} 
							node={node} 
							onToggleExpand={() => this.onToggleExpand(index)} />
					})}
				</tbody>
			</table>
		</div>
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
						{ name: 'Header', weight: 5, kids: [
							{ name: 'Depth', weight: 5 }
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
							{ name: 'Type', weight: 4, kids: [
								{
									name: 'S1_1', kids: [
										{
											name: 'A3', kids: [
												{
													name: 'TheDataModel', kids: [
														{
															name: 'Header', weight: 5, kids: [
																{ name: 'Depth', weight: 5 }
															]
														},
														{
															name: 'Data', kids: [
																{ name: 'Type', weight: 4 }
															]
														}
													]
												}
											]
										},
										{
											name: 'A4', weight: 2, kids: [
												{
													name: 'TheDataModel', kids: [
														{
															name: 'Header', weight: 1, kids: [
																{ name: 'Depth', weight: 5 }
															]
														},
														{
															name: 'Data', kids: [
																{ name: 'Type', weight: 4, kids: [
																	{
																		name: 'S1_1', kids: [
																			{
																				name: 'A3', kids: [
																					{
																						name: 'TheDataModel', kids: [
																							{
																								name: 'Header', weight: 5, kids: [
																									{ name: 'Depth', weight: 5 }
																								]
																							},
																							{
																								name: 'Data', kids: [
																									{ name: 'Type', weight: 4, kids: [
																										{
																											name: 'S1_1', kids: [
																												{
																													name: 'A3', kids: [
																														{
																															name: 'TheDataModel', kids: [
																																{
																																	name: 'Header', weight: 5, kids: [
																																		{ name: 'Depth', weight: 5 }
																																	]
																																},
																																{
																																	name: 'Data', kids: [
																																		{ name: 'Type', weight: 4 }
																																	]
																																}
																															]
																														}
																													]
																												},
																												{
																													name: 'A4', weight: 2, kids: [
																														{
																															name: 'TheDataModel', kids: [
																																{
																																	name: 'Header', weight: 1, kids: [
																																		{ name: 'Depth', weight: 5 }
																																	]
																																},
																																{
																																	name: 'Data', kids: [
																																		{ name: 'Type', weight: 4 }
																																	]
																																}
																															]
																														}
																													]
																												}
																											]
																										},
																										{
																											name: 'S4_1', kids: [
																												{ name: 'Item1', weight: 1 },
																												{ name: 'Item2', weight: 0 }
																											]
																										}

																									] }
																								]
																							}
																						]
																					}
																				]
																			},
																			{
																				name: 'A4', weight: 2, kids: [
																					{
																						name: 'TheDataModel', kids: [
																							{
																								name: 'Header', weight: 1, kids: [
																									{ name: 'Depth', weight: 5 }
																								]
																							},
																							{
																								name: 'Data', kids: [
																									{ name: 'Type', weight: 4 }
																								]
																							}
																						]
																					}
																				]
																			}
																		]
																	},
																	{
																		name: 'S4_1', kids: [
																			{ name: 'Item1', weight: 1 },
																			{ name: 'Item2', weight: 0 }
																		]
																	}

																] }
															]
														}
													]
												}
											]
										}
									]
								},
								{
									name: 'S4_1', kids: [
										{ name: 'Item1', weight: 1 },
										{ name: 'Item2', weight: 0 }
									]
								}

							]}
						]}
					]}
				]}
			]},
			{ name: 'S4_1', kids: [
				{ name: 'Item1', weight: 1 },
				{ name: 'Item2', weight: 0 }
			]}
		];

		function recurseChecked(data, prefix) {
			return _.flatMap(data, item => {
				const key = `${prefix}.${item.name}`;
				const weight = _.get(item, 'weight', 3);
				const checked = weight > 0;
				const result = [];
				if (checked) {
					result.push(key);
				}
				return result.concat(recurseChecked(item.kids || [], key));
			});
		}

		function recurseNodes(data, prefix) {
			return data.map(item => {
				const key = `${prefix}.${item.name}`;
				const weight = _.get(item, 'weight', 3);
				const className = `bg-weight-${weight}`;
				const title = <div style={{ width: 200 }}>
					{item.name}
				</div>
				if (item.kids) {
					return <Tree.TreeNode title={title} key={key}>
						{recurseNodes(item.kids, key) }
					</Tree.TreeNode>
				}
				return <Tree.TreeNode title={title} key={key} />;
			});
		}

		const checked = recurseChecked(data, '$');
		const nodes = recurseNodes(data, '$');

		return <div>
			<Tree showIcon={false} 
				defaultExpandAll 
				selectable={false} 
				defaultCheckedKeys={checked}>
				{nodes}
			</Tree>
			<TreeView data={data} />
		</div>
	}
}

export default Tuning;
