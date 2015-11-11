/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IPitTestScope extends IViewModelScope {
		Title: string;
	}

	export class PitTestController {

		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Pit,
			C.Services.Test,
			C.Services.Wizard
		];

		constructor(
			$scope: IPitTestScope,
			private $state: ng.ui.IStateService,
			private pitService: PitService,
			private testService: TestService,
			private wizardService: WizardService
		) {
			$scope.Title = "Test";
			this.pitService.LoadPit();
			this.track = this.wizardService.GetTrack(C.Tracks.Test);
		}

		private track: ITrack;
		public Tracks: ITrackStatic[] = _.filter(WizardTracks, 'incompleteMsg');
		public Title = 'Test';

		private get isWizard(): boolean {
			return this.$state.is(C.States.PitWizardTest);
		}

		public get ShowNotConfigured(): boolean {
			return !this.pitService.IsConfigured;
		}

		public get ShowNoMonitors(): boolean {
			return this.pitService.IsConfigured && !this.pitService.HasMonitors;
		}

		public get CanWizardBeginTest(): boolean {
			return this.CanBeginTest && this.IsComplete(C.Tracks.Fault);
		}

		public get CanBeginTest(): boolean {
			return this.pitService.IsConfigured && this.testService.CanBeginTest;
		}

		public get CanContinue(): boolean {
			return onlyIf(this.testService.TestResult, () => { 
				return this.testService.CanBeginTest &&
					this.testService.TestResult.status === TestStatus.Pass;
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
