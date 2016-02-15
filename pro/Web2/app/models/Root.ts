import { RouterState } from './Router';
import { LibraryState } from './Library';
import { Pit } from './Pit';
import { Job } from './Job';
import { TestState } from './PitTest';
import { FaultListState, FaultDetail } from './Fault';

interface RootState {
	error?: string;
	library?: LibraryState;
	router?: RouterState;
	pit?: Pit;
	test?: TestState;
	job?: Job;
	jobs?: Job[];
	faults?: FaultListState;
	fault?: FaultDetail;
	await?: {
		statuses: {
			library: string;
			router: string;
			pit: string;
			test: string;
			jobs: string;
			job: string;
			faults: string;
			fault: string;
		};
		errors: {
		};
	};
}

export interface GetState {
	(): RootState;
}

export default RootState;
