export interface FaultListState {
	isPolling?: boolean;
	data?: FaultSummary[];
}

export interface FaultSummary {
	id: number;
	faultUrl: string;
	archiveUrl: string;
	reproducible: boolean;
	iteration: number;
	timeStamp: string;
	source: string;
	exploitability: string;
	majorHash: string;
	minorHash: string;
}

export interface FaultDetail extends FaultSummary {
	nodeUrl?: string;
	targetUrl?: string;
	targetConfigUrl?: string;
	pitUrl: string;
	peachUrl?: string;

	title: string;
	description: string;
	seed: number;
	files: FaultFile[];

	// range of search when fault was found
	iterationStart: number;
	iterationStop: number;
}

export interface FaultFile {
	name: string;
	fullName: string;
	fileUrl: string;
	size: number;
}
