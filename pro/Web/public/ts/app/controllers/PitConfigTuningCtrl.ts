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
		style: any;
		weightIcons: string[];
		expanderIcon: string;
		showExpander: boolean;
	}

	function defaultWeight(node: IPitFieldNode) {
		return _.isUndefined(node.weight) ? 3 : node.weight;
	}

	function flatten(nodes: IPitFieldNode[], depth: number, parent: FlatNode) {
		return _.flatMap(nodes, (node: IPitFieldNode) => {
			const expanded = _.isUndefined(node.expanded) ? 
				depth < 2 : 
				node.expanded;
			const visible = !parent || parent.expanded && parent.visible;
			const weight = defaultWeight(node);
			const icons = _.range(6).map(i => {
				return (weight === i) ? 'fa-circle' : 
					(!expanded && matchWeight(node, i)) ? 'fa-dot-circle-o' :
						'fa-circle-o';
			});
			const flat: FlatNode = {
				node: node,
				id: node.id,
				weight: weight,
				depth: depth,
				visible: visible,
				expanded: expanded,
				style: { 'margin-left': depth * SHIFT_WIDTH },
				weightIcons: icons,
				expanderIcon: expanded ? 'fa-minus' : 'fa-plus',
				showExpander: !_.isEmpty(node.fields)
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
			console.time('load');
			const promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				console.timeEnd('load');
				this.tree = _.cloneDeep(pit.metadata.fields);
				console.time('flatten');
				this.flat = flatten(this.tree, 0, null);
				console.timeEnd('flatten');
				console.log('nodes', this.flat.length);
				this.hasLoaded = true;
			});
		}

		private hasLoaded: boolean = false;
		private flat: FlatNode[];
		private tree: IPitFieldNode[] = [];

		OnToggleExpand(node: FlatNode) {
			node.node.expanded = !node.expanded;
			console.time('expand');
			this.flat = flatten(this.tree, 0, null);
			console.timeEnd('expand');
		}

		OnSelectWeight(node: FlatNode, weight: number) {
			selectWeight(node.node, weight);
			console.time('select');
			this.flat = flatten(this.tree, 0, null);
			console.timeEnd('select');
		}
	}
}
