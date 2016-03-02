/// <reference path="../reference.ts" />

namespace Peach {
	const SHIFT_WIDTH = 20;
	const MAX_NODES = 2000;

	export interface FlatNode {
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
		direct: boolean;
	}

	function defaultWeight(node: IPitFieldNode) {
		return _.isUndefined(node.weight) ? 3 : node.weight;
	}

	function flatten(nodes: IPitFieldNode[], depth: number, parent: FlatNode, collect: FlatNode[]) {
		nodes.forEach(node => {
			const expanded = _.isUndefined(node.expanded) ?
				depth < 2 :
				node.expanded;
			const visible = !parent || parent.expanded && parent.visible;
			const weight = defaultWeight(node);
			const icons = _.range(6).map(i => {
				return (weight === i) ? 'fa-circle' :
					(!expanded && matchWeight(node, i)) ? 
						'fa-dot-circle-o' :
						'fa-circle-thin';
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
				showExpander: !_.isEmpty(node.fields),
				direct: false
			};
			collect.push(flat);
			flatten(node.fields, depth + 1, flat, collect);
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

	export interface ITuningScope extends IFormScope {
		flat: FlatNode[];
		hasLoaded: boolean;
		isTruncated: boolean;
		MAX_NODES: number;
	}

	function applyWeights(weights: IPitWeight[], fields: IPitFieldNode[]) {
		console.time('applyWeights');
		for (const rule of weights) {
			const parts = rule.id.split('.');
			applyWeight(fields, parts, rule.weight);
		}
		console.timeEnd('applyWeights');
	}

	function applyWeight(fields: IPitFieldNode[], parts: string[], weight: number) {
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

	function extractWeights(prefix: string, tree: IPitFieldNode[], collect: IPitWeight[]) {
		for (const node of tree) {
			const here = `${prefix}${node.id}`;
			if (defaultWeight(node) !== 3) {
				collect.push({id: here, weight: node.weight});
			}
			extractWeights(`${here}.`, node.fields, collect);
		}
	}

	function cloneFields(fields: IPitFieldNode[]): IPitFieldNode[] {
		return fields.map(item => ({ 
			id: item.id, 
			fields: cloneFields(item.fields) 
		}));
	}

	export class ConfigureTuningController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		private isSaved = false;
		private tree: IPitFieldNode[] = [];
		private nodeHover: FlatNode = null;
		private hovers: boolean[] = [
			false,
			false,
			false,
			false,
			false,
			false
		];

		constructor(
			private $scope: ITuningScope,
			private pitService: PitService
		) {
			this.$scope.hasLoaded = false;
			this.$scope.isTruncated = false;
			this.$scope.MAX_NODES = MAX_NODES;

			console.time('load');
			const promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				console.time('clone');
				this.tree = cloneFields(pit.metadata.fields);
				console.timeEnd('clone');
				applyWeights(pit.weights, this.tree);

				this.flatten();
				this.$scope.hasLoaded = true;
				setTimeout(() => console.timeEnd('load'));
			});
		}

		flatten() {
			console.time('flatten');
			const flat = [];
			flatten(this.tree, 0, null, flat);
			console.timeEnd('flatten');

			console.log('nodes', flat.length);
			this.$scope.isTruncated = (flat.length > MAX_NODES);
			this.$scope.flat = _.take(flat, MAX_NODES);
		}

		LegendText(i: number) {
			return this.hovers[i] ? 'text bold' : 'text';
		}

		LegendIcon(i: number) {
			return this.hovers[i] ? 'fa-circle' : 'fa-circle-thin';
		}

		OnLegendEnter(i: number) {
			this.hovers[i] = true;
		}

		OnLegendLeave(i: number) {
			this.hovers[i] = false;
		}

		RowHover(node: FlatNode) {
			return !_.isNull(this.nodeHover) && (node.node === this.nodeHover.node) ? 
				'tuning-row-hover' : 
				'';
		}

		OnRowEnter(node: FlatNode) {
			this.nodeHover = node;
		}

		OnRowLeave(node: FlatNode) {
			this.nodeHover = null;
		}

		OnToggleExpand(node: FlatNode) {
			node.node.expanded = !node.expanded;
			node.expanderIcon = 'fa-spin fa-clock-o';
			setTimeout(() => {
				this.flatten();
				setTimeout(() => {
					this.$scope.$apply();
				});
			}, 100);
		}

		OnSelectWeight(node: FlatNode, weight: number) {
			node.weightIcons[weight] = 'fa-spin fa-clock-o';
			selectWeight(node.node, weight);
			setTimeout(() => {
				this.flatten();
				this.$scope.form.$setDirty();
				setTimeout(() => {
					this.$scope.$apply();
				});
			}, 100);
		}

		public get ShowSaved(): boolean {
			return !this.$scope.form.$dirty  && this.isSaved;
		}

		public get CanSave(): boolean {
			return this.$scope.form.$dirty;
		}

		public OnSave(): void {
			const weights = [];
			console.time('extractWeights');
			extractWeights('', this.tree, weights);
			console.timeEnd('extractWeights');
			console.log(weights.length);

			const promise = this.pitService.SaveWeights(weights);
			promise.then(() => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}
	}
}
