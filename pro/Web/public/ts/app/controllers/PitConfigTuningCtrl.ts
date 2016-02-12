/// <reference path="../reference.ts" />

namespace Peach {
	interface Node {
		name: string;
		weight?: number;
		kids?: Node[];
		expanded?: boolean;
	}

	interface FlatNode {
		node: Node;
		name: string;
		weight: number;
		depth: number;
		visible: boolean;
		expanded: boolean;
	}

	function defaultWeight(node: Node) {
		return _.isUndefined(node.weight) ? 3 : node.weight;
	}

	function flatten(nodes: Node[], depth: number, parent: FlatNode) {
		return _.flatMap(nodes, (node: Node) => {
			const expanded = _.isUndefined(node.expanded) ? true : node.expanded;
			const visible = !parent || parent.expanded && parent.visible;
			const flat: FlatNode = {
				node: node,
				name: node.name,
				weight: defaultWeight(node),
				depth: depth,
				visible: visible,
				expanded: expanded
			};			
			return [flat].concat(flatten(node.kids, depth + 1, flat));
		});
	}

	function matchWeight(node: Node, weight: number) {
		return (defaultWeight(node) === weight) ||
			_.some(node.kids, kid => matchWeight(kid, weight));
	}

	function selectWeight(node: Node, weight: number) {
		node.weight = weight;
		const kids = node.kids || [];
		kids.forEach(kid => selectWeight(kid, weight));
	}

	export class ConfigureTuningController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IViewModelScope,
			private pitService: PitService
		) {
			const promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				this.hasLoaded = true;
			});

			this.flat = flatten(this.tree, 0, null);
		}

		private hasLoaded: boolean = false;
		private flat: FlatNode[];
		private tree: Node[] = [
			{ name: 'A', weight: 0 },
			{ name: 'B', weight: 1, kids: [
				{ name: 'B1', weight: 2, kids: [
					{ name: 'X', kids: [
						{ name: 'Z' }
					]},
					{ name: 'Y' }
				]},
				{ name: 'B2', weight: 3 }
			]},
			{ name: 'C', weight: 4, kids: [
				{ name: 'C1', weight: 5 },
				{ name: 'C2', weight: 5 }
			]}
		];

		ShowNode(node: FlatNode) {
			return node.visible;
		}

		WeightIcon(node: FlatNode, weight: number) {
			return {
				'fa-circle': (node.weight === weight),
				'fa-circle-o': (node.weight !== weight),
				'fa-dot-circle-o': (node.weight !== weight) && 
					!node.expanded && matchWeight(node.node, weight)
			}
		}

		ShowExpander(node: FlatNode) {
			return node.node.kids;
		}

		ExpanderIcon(node: FlatNode) {
			return {
				'fa-minus': node.expanded,
				'fa-plus': !node.expanded
			}
		}

		NodeShift(node: FlatNode) {
			return {
				'margin-left': node.depth * 30
			}
		}

		OnToggleExpand(node: FlatNode) {
			node.node.expanded = !node.expanded;
			this.flat = flatten(this.tree, 0, null);
		}

		OnSelectWeight(node: FlatNode, weight: number) {
			selectWeight(node.node, weight);
			this.flat = flatten(this.tree, 0, null);
		}
	}
}
