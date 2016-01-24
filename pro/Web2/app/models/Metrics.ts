import { StateContainer } from './Root';

export interface MetricsState {
	faultTimeline: FaultTimelineMetric[];
	bucketTimeline: BucketTimelineMetric[];
	mutators: MutatorMetric[];
	elements: ElementMetric[];
	states: StateMetric[];
	dataset: DatasetMetric[];
	buckets: BucketMetric[];
}

export interface FaultTimelineMetric {
	date: Date;
	faultCount: number;
}

export interface BucketTimelineMetric {
	id: number;
	label: string;
	iteration: number;
	time: Date;
	faultCount: number;
	href: string;
}

export interface MutatorMetric {
	mutator: string;
	elementCount: number;
	iterationCount: number;
	bucketCount: number;
	faultCount: number;
}

export interface ElementMetric {
	state: string;
	action: string;
	element: string;
	iterationCount: number;
	mutationCount: number;
	bucketCount: number;
	faultCount: number;
}

export interface StateMetric {
	state: string;
	executionCount: number;
}

export interface DatasetMetric {
	dataset: string;
	iterationCount: number;
	bucketCount: number;
	faultCount: number;
}

export interface BucketMetric {
	id?: any;
	bucket: string;
	mutator: string;
	dataset: string;
	state: string;
	action: string;
	element: string;
	iterationCount: number;
	faultCount: number;
}

export interface VisualizerData {
	iteration: number;
	mutatedElements: string[];
	models: VisualizerModel[];
}

export interface VisualizerModel {
	original: string;
	fuzzed: string;
	name: string;
	type: string;
	children: VisualizerModelChild[];
}

export interface VisualizerModelChild {
	name: string;
	type: string;
	children: VisualizerModelChild[];
}
