/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface ITrackTestItem {
		track: string;
		title: string;
		message: string;
		link?: string;
		fail?: boolean;
	}


	var TestTracks: ITrackTestItem[] = [
		{
			track: C.Tracks.Vars,
			title: 'Set Variables',
			message: 'Not completed, default values will be used.'
		},
		{
			track: C.Tracks.Fault,
			title: 'Fault Detection',
			message: 'Fault Detection requires completion.',
			link: 'Go to Fault Detection',
			fail: true
		},
		{
			track: C.Tracks.Data,
			title: 'Data Collection',
			message: 'No Data Collection will be performed.'
		},
		{
			track: C.Tracks.Auto,
			title: 'Automation',
			message: 'No Automation will be performed.'
		}
	];

	export class PitTestController {

		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Pit,
			C.Services.Test,
			C.Services.Wizard
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private pitService: PitService,
			private testService: TestService,
			private wizardService: WizardService
		) {
			this.track = this.wizardService.GetTrack(C.Tracks.Test);
		}

		private track: ITrack;
		public Tracks: ITrackTestItem[] = TestTracks;

		private get isWizard(): boolean {
			return this.$state.is(C.States.WizardTrack, { track: C.Tracks.Test });
		}

		public get ShowNotConfigured(): boolean {
			return !this.pitService.IsConfigured;
		}

		public get CanWizardBeginTest(): boolean {
			return this.CanBeginTest && this.IsComplete(C.Tracks.Fault);
		}

		public get CanBeginTest(): boolean {
			return this.testService.CanBeginTest;
		}

		public get CanContinue() {
			return this.testService.TestResult.status === TestStatus.Pass;
		}

		public IsComplete(track: string): boolean {
			return this.wizardService.GetTrack(track).isComplete;
		}

		public TrackStatus(item: ITrackTestItem) {
			return this.IsComplete(item.track) ?
				'label-success' : item.fail ?
					'label-danger' : 'label-warning';
		}

		public TrackMessage(item: ITrackTestItem) {
			return this.IsComplete(item.track) ? 'Complete' : item.message;
		}

		public TrackLink(item: ITrackTestItem) {
			return this.IsComplete(item.track) ? false : _.isString(item.link);
		}

		public OnBeginTest() {
			var agents = [
				this.wizardService.GetTrack(C.Tracks.Fault).agents,
				this.wizardService.GetTrack(C.Tracks.Data).agents,
				this.wizardService.GetTrack(C.Tracks.Auto).agents
			];

			this.track.isComplete = false;
			var promise = this.pitService.SaveAgents(_.flatten<Agent>(agents));
			promise.then(() => {
				var promise2 = this.testService.BeginTest();
				promise2.then(() => {
					if (this.isWizard) {
						this.track.isComplete = true;
					}
				});
			});
		}

		public OnNextTrack() {
			this.$state.go(C.States.Home);
		}
	}
}
