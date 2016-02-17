/// <reference path="../reference.ts" />

namespace Peach {
	const SHIFT_WIDTH = 20;

	interface FlatNode {
		node: IPitFieldNode;
		id: string;
		weight: number;
		depth: number;
		visible: boolean;
		expanded: boolean;
	}

	function defaultWeight(node: IPitFieldNode) {
		return _.isUndefined(node.weight) ? 3 : node.weight;
	}

	function flatten(nodes: IPitFieldNode[], depth: number, parent: FlatNode) {
		return _.flatMap(nodes, (node: IPitFieldNode) => {
			const expanded = _.isUndefined(node.expanded) ? true : node.expanded;
			const visible = !parent || parent.expanded && parent.visible;
			const flat: FlatNode = {
				node: node,
				id: node.id,
				weight: defaultWeight(node),
				depth: depth,
				visible: visible,
				expanded: expanded
			};			
			return [flat].concat(flatten(node.fields, depth + 1, flat));
		});
	}

	function matchWeight(node: IPitFieldNode, weight: number) {
		return (defaultWeight(node) === weight) ||
			_.some(node.fields, field => matchWeight(field, weight));
	}

	function selectWeight(node: IPitFieldNode, weight: number) {
		node.weight = weight;
		const fields = node.fields || [];
		fields.forEach(field => selectWeight(field, weight));
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
				this.tree = _.cloneDeep(pit.metadata.fields);
				this.flat = flatten(this.tree, 0, null);
				this.hasLoaded = true;
			});
		}

		private hasLoaded: boolean = false;
		private flat: FlatNode[];
		private tree: IPitFieldNode[] = [];

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
			return node.node.fields;
		}

		ExpanderIcon(node: FlatNode) {
			return {
				'fa-minus': node.expanded,
				'fa-plus': !node.expanded
			}
		}

		NodeShift(node: FlatNode) {
			return {
				'margin-left': node.depth * SHIFT_WIDTH
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
