import React = require('react');
import { ComponentClass, ReactNode } from 'react';
import { Badge } from 'react-bootstrap';

import RootState from '../models/root';

import CMain from './Main';
import CError from './Error';

import CMainMenu from './main/MainMenu';
import CHome from './main/Home';
import CLibrary from './main/Library';
import CJobs from './main/Jobs';

import CPitsMenu from './pits/PitsMenu';
import CPitMount from './pits/Pit';
import CPitDashboard from './pits/Dashboard';
import CPitWizardIntro from './pits/wizard/Intro';
import CPitWizardTrack from './pits/wizard/Track';
import CPitWizardVarsIntro from './pits/wizard/vars/Intro';
import CPitWizardFaultIntro from './pits/wizard/fault/Intro';
import CPitWizardDataIntro from './pits/wizard/data/Intro';
import CPitWizardAutoIntro from './pits/wizard/auto/Intro';
import CPitWizardTest from './pits/wizard/Test';
import CPitAdvancedVariables from './pits/advanced/Variables';
import CPitAdvancedMonitoring from './pits/advanced/Monitoring';
import CPitAdvancedTuning from './pits/advanced/Tuning';
import CPitAdvancedTest from './pits/advanced/Test';

import CJobsMenu from './jobs/JobsMenu';
import CJob from './jobs/Job';
import CJobDashboard from './jobs/Dashboard';
import CJobFaults from './jobs/Faults';
import CJobFaultsDetail from './jobs/FaultsDetail';
import CBucketTimeline from './jobs/metrics/BucketTimeline';
import CFaultTimeline from './jobs/metrics/FaultTimeline';
import CMutators from './jobs/metrics/Mutators';
import CElements from './jobs/metrics/Elements';
import CStates from './jobs/metrics/States';
import CDataset from './jobs/metrics/Dataset';
import CBuckets from './jobs/metrics/Buckets';

export type DisplayNameFunc = (state: RootState) => string;
export type ExtraFunc = (state: RootState) => ReactNode;

export interface RouteSpec {
	name?: string;
	path: string;
	parts?: ComponentClass<any>[];
	label?: string;
	icon?: string;
	displayName?: string | DisplayNameFunc;
	redirect?: string;
	abstract?: boolean;
	extra?: ExtraFunc;
}

export const NotFound: RouteSpec = {
	path: '',
	parts: [CMainMenu, CError],
	displayName: 'Not Found'
};

export const R = {
	Root: {
		name: 'root',
		path: '/',
		parts: [CMainMenu, CHome],
		displayName: 'Home',
		icon: 'home',
	},
	Library: {
		name: 'root.pits',
		path: 'pits',
		parts: [CMainMenu, CLibrary],
		displayName: 'Library',
		icon: 'book'
	},
	Jobs: {
		name: 'root.jobs',
		path: 'jobs',
		parts: [CMainMenu, CJobs],
		displayName: 'Jobs',
		icon: 'history'
	},
	Pit: {
		name: 'root.pits.pit',
		path: '/:pit',
		parts: [CPitsMenu, CPitMount, CPitDashboard],
		label: 'Pit',
		icon: 'sliders',
		displayName: (state: RootState) => (
			(state.await.statuses.pit === 'success') ?
				state.pit.name :
				'Loading...'
		)
	},
	PitWizard: {
		name: 'root.pits.pit.wizard',
		path: '/quickstart',
		parts: [CPitsMenu, CPitMount, CPitWizardIntro],
		displayName: 'Quick Start',
		icon: 'rocket'
	},
	PitWizardVars: {
		name: 'root.pits.pit.wizard.vars',
		path: '/vars',
		parts: [CPitsMenu, CPitMount, CPitWizardTrack, CPitWizardVarsIntro],
		displayName: 'Set Variables'
	},
	PitWizardFault: {
		name: 'root.pits.pit.wizard.fault',
		path: '/fault',
		parts: [CPitsMenu, CPitMount, CPitWizardTrack, CPitWizardFaultIntro],
		displayName: 'Fault Detection'
	},
	PitWizardData: {
		name: 'root.pits.pit.wizard.data',
		path: '/data',
		parts: [CPitsMenu, CPitMount, CPitWizardTrack, CPitWizardDataIntro],
		displayName: 'Data Collection'
	},
	PitWizardAuto: {
		name: 'root.pits.pit.wizard.auto',
		path: '/auto',
		parts: [CPitsMenu, CPitMount, CPitWizardTrack, CPitWizardAutoIntro],
		displayName: 'Automation'
	},
	PitWizardTest: {
		name: 'root.pits.pit.wizard.test',
		path: '/test',
		parts: [CPitsMenu, CPitMount, CPitWizardTest],
		displayName: 'Test'
	},
	PitAdvanced: {
		name: 'root.pits.pit.advanced',
		path: '/advanced',
		abstract: true,
		redirect: 'root.pits.pit',
		displayName: 'Configure',
		icon: 'wrench'
	},
	PitAdvancedVariables: {
		name: 'root.pits.pit.advanced.variables',
		path: '/variables',
		parts: [CPitsMenu, CPitMount, CPitAdvancedVariables],
		displayName: 'Variables'
	},
	PitAdvancedMonitoring: {
		name: 'root.pits.pit.advanced.monitoring',
		path: '/monitoring',
		parts: [CPitsMenu, CPitMount, CPitAdvancedMonitoring],
		displayName: 'Monitoring'
	},
	PitAdvancedTuning: {
		name: 'root.pits.pit.advanced.tuning',
		path: '/tuning',
		parts: [CPitsMenu, CPitMount, CPitAdvancedTuning],
		displayName: 'Tuning'
	},
	PitAdvancedTest: {
		name: 'root.pits.pit.advanced.test',
		path: '/test',
		parts: [CPitsMenu, CPitMount, CPitAdvancedTest],
		displayName: 'Test'
	},
	Job: {
		name: 'root.jobs.job',
		path: '/:job',
		parts: [CJobsMenu, CJob, CJobDashboard],
		label: 'Dashboard',
		icon: 'dashboard',
		displayName: (state: RootState) =>
			(state.await.statuses.job === 'success') ?
				state.job.name :
				'Loading...'
	},
	JobFaults: {
		name: 'root.jobs.job.faults',
		path: '/faults',
		parts: [CJobsMenu, CJob, CJobFaults],
		displayName: 'Faults',
		icon: 'flag',
		extra: (state: RootState) => <Badge>
			{state.faults.data.length}
		</Badge>
	},
	JobFaultsDetail: {
		name: 'root.jobs.job.faults.detail',
		path: '/:fault',
		parts: [CJobsMenu, CJob, CJobFaultsDetail],
		displayName: (state: RootState) =>
			(state.await.statuses.fault === 'success') ?
				`Test Case #${state.fault.iteration}` :
				'Loading...'
	},
	JobMetrics: {
		name: 'root.jobs.job.metrics',
		path: '/metrics',
		abstract: true,
		redirect: 'root.jobs.job',
		displayName: 'Metrics',
		icon: 'bar-chart'
	},
	JobMetricsBucketTimeline: {
		name: 'root.jobs.job.metrics.bucketTimeline',
		path: '/bucketTimeline',
		parts: [CJobsMenu, CJob, CBucketTimeline],
		displayName: 'Buckets Timeline'
	},
	JobMetricsFaultTimeline: {
		name: 'root.jobs.job.metrics.faultTimeline',
		path: '/faultTimeline',
		parts: [CJobsMenu, CJob, CFaultTimeline],
		displayName: 'Faults Timeline'
	},
	JobMetricsMutators: {
		name: 'root.jobs.job.metrics.mutators',
		path: '/mutators',
		parts: [CJobsMenu, CJob, CMutators],
		displayName: 'Mutators'
	},
	JobMetricsElements: {
		name: 'root.jobs.job.metrics.elements',
		path: '/elements',
		parts: [CJobsMenu, CJob, CElements],
		displayName: 'Elements'
	},
	JobMetricsStates: {
		name: 'root.jobs.job.metrics.states',
		path: '/states',
		parts: [CJobsMenu, CJob, CStates],
		displayName: 'States'
	},
	JobMetricsDataset: {
		name: 'root.jobs.job.metrics.dataset',
		path: '/dataset',
		parts: [CJobsMenu, CJob, CDataset],
		displayName: 'Dataset'
	},
	JobMetricsBuckets: {
		name: 'root.jobs.job.metrics.buckets',
		path: '/buckets',
		parts: [CJobsMenu, CJob, CBuckets],
		displayName: 'Buckets'
	},
	Docs: {
		path: '/docs',
		label: 'Help',
		icon: 'question'
	},
	Forums: {
		path: 'https://forums.peachfuzzer.com',
		label: 'Forums',
		icon: 'comment'
	},
	Error: {
		name: 'error',
		path: '/error',
		parts: [CMainMenu, CError],
		displayName: 'Error'
	}
};

export const routes = _.values<RouteSpec>(_.filter(R, 'name'));
export const segments = _.keyBy(routes, 'name');
