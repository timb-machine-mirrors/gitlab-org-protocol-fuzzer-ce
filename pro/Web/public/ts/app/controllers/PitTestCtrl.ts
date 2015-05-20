/// <reference path="../reference.ts" />

module Peach {
	"use strict";

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
			this.pitService.LoadPit();
		}

		private track: ITrack;
		public Tracks: ITrackStatic[] = _.filter(WizardTracks, 'incompleteMsg');
		public Title = 'Test';

		private get isWizard(): boolean {
			return this.$state.is(C.States.PitWizard, { track: C.Tracks.Test });
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

		public get CanContinue(): boolean {
			return onlyIf(this.testService.TestResult, () => { 
				return this.testService.TestResult.status === TestStatus.Pass;
			}) || false;
		}

		public IsComplete(track: string): boolean {
			return this.wizardService.GetTrack(track).isComplete;
		}

		public TrackStatus(item: ITrackStatic) {
			return this.IsComplete(item.id) ?
				'label-success' : item.fail ?
					'label-danger' : 'label-warning';
		}

		public TrackMessage(item: ITrackStatic) {
			return this.IsComplete(item.id) ? 'Complete' : item.incompleteMsg;
		}

		public TrackLink(item: ITrackStatic) {
			return this.IsComplete(item.id) ? false : _.isString(item.link);
		}

		public OnBeginTest() {
			if (this.isWizard) {
				this.track.isComplete = false;
				var agents = [
					this.wizardService.GetTrack(C.Tracks.Fault).agents,
					this.wizardService.GetTrack(C.Tracks.Data).agents,
					this.wizardService.GetTrack(C.Tracks.Auto).agents
				];
				var promise = this.pitService.SaveAgents(_.flatten<Agent>(agents));
				promise.then(() => {
					this.startTest();
				});
			} else {
				this.startTest();
			}
		}

		private startTest() {
			var promise = this.testService.BeginTest();
			promise.then(() => {
				if (this.isWizard) {
					this.track.isComplete = true;
				}
			});
		}

		public OnNextTrack() {
			this.$state.go(this.track.next.state);
		}
	}
}
