import superagent = require('superagent');

import { Library } from '../models/Library';
import { Job, JobRequest } from '../models/Job';
import { Pit } from '../models/Pit';
import { TestResult } from '../models/PitTest';
import { FaultSummary, FaultDetail } from '../models/Fault';

function request<T>(method: string, url: string, errHeader: string, data?, query?) {
	return new Promise<T>((resolve, reject) => {
		superagent(method, url)
			.query(query)
			.type('json')
			.accept('json')
			.send(data)
			.end((err, res) => {
				if (err) {
					reject(`${errHeader}: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}

export function fetchLibraries() {
	return request('get', '/p/libraries', 'Failed to start libraries');
}

export function fetchPit(id: string) {
	return request<Pit>('get', `/p/pits/${id}`, 'Failed to load pit');
}

export function savePit(pit: Pit) {
	return request<Pit>('post', pit.pitUrl, 'Failed to save pit', pit);
}

export function fetchTestResult(job: Job) {
	return request<TestResult>('get', job.firstNodeUrl, 'Test result failed to load');
}

export function fetchJobs() {
	return request<Job[]>('get', '/p/jobs', 'Failed to load job listing', undefined, { dryrun: false });
}

export function fetchJob(id: string) {
	return request<Job>('get', `/p/jobs/${id}`, 'Failed to load job');
}

export function startJob(jobRequest: JobRequest) {
	return request<Job>('post', '/p/jobs', 'Failed to start job', jobRequest);
}

export function startTest(pit: Pit) {
	return request<Job>('post', '/p/jobs', 'Failed to start test', { pitUrl: pit.pitUrl, dryrun: true });
}

export function stopTest(job: Job) {
	return request('get', job.commands.stopUrl, `Failed to stop test`);
}

export function sendJobCommand(cmd: string, url: string) {
	return request('get', url, `Failed to ${cmd} job`);
}

export function deleteJob(job: Job) {
	return request('delete', job.jobUrl, 'Job failed to delete');
}

export function fetchFaults(job: Job) {
	return request<FaultSummary[]>('get', job.faultsUrl, 'Fault listing failed to load');
}

export function fetchFault(params) {
	const { job, fault } = params;
	const url = `/p/jobs/${job}/faults/${fault}`;
	return request<FaultDetail>('get', url, 'Fault failed to load');
}

export function fetchMetric(job: Job, metric: string) {
	return new Promise((resolve, reject) => {
		const url = job.metrics[metric];
		superagent.get(url)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Metric '${metric}' failed to load: ${err.message}`);
				} else {
					resolve({
						metric,
						data: res.body
					});
				}
			})
		;
	});
}
