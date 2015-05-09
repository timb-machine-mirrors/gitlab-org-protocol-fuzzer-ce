/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class MainController {

		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Angular.$modal,
			C.Services.Pit,
			C.Services.Test,
			C.Services.Job,
			C.Services.Wizard
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService,
			private testService: TestService,
			private jobService: JobService,
			private wizardService: WizardService
		) {
			$scope.vm = this;

			$scope.$root.$on(C.Angular.$stateChangeSuccess, () => {
				this.subMenus.forEach(item => {
					if ($state.includes(item.state)) {
						item.collapsed = false;
					} else {
						item.collapsed = true;
					}
				});
			});

			var promise = this.jobService.GetJobs();
			promise.then(() => {
				if (_.isUndefined(this.job)) {
					if (!this.pitService.RestorePit()) {
						this.OnSelectPit();
					}
				}
			});
		}

		private get pit(): IPit {
			return this.pitService.Pit;
		}

		private get job(): IJob {
			return this.jobService.Job;
		}

		public get JobId(): number {
			return 1;
		}

		public get PitId(): number {
			return 1;
		}

		public Metrics = [
			{ id: C.Metrics.BucketTimeline, name: 'Bucket Timeline' },
			{ id: C.Metrics.FaultTimeline, name: 'Faults Over Time' },
			{ id: C.Metrics.Mutators, name: 'Mutators' },
			{ id: C.Metrics.Elements, name: 'Elements' },
			{ id: C.Metrics.States, name: 'States' },
			{ id: C.Metrics.Dataset, name: 'Datasets' },
			{ id: C.Metrics.Buckets, name: 'Buckets' }
		];

		// TODO: use WizardService definition of tracks
		public WizardTracks = [
			{ id: C.Tracks.Intro, name: 'Introduction', state: C.States.PitWizard },
			{ id: C.Tracks.Vars, name: 'Set Variables', state: C.States.PitWizardIntro },
			{ id: C.Tracks.Fault, name: 'Fault Detection', state: C.States.PitWizardIntro },
			{ id: C.Tracks.Data, name: 'Data Collection', state: C.States.PitWizardIntro },
			{ id: C.Tracks.Auto, name: 'Automation', state: C.States.PitWizardIntro },
			{ id: C.Tracks.Test, name: 'Test', state: C.States.PitWizard }
		];

		public ConfigSteps = [
			{ id: C.States.PitConfigVariables, name: 'Variables' },
			{ id: C.States.PitConfigMonitoring, name: 'Monitoring' },
			{ id: C.States.PitConfigTest, name: 'Test' }
		];

		public OnItemClick(event: ng.IAngularEvent, enabled) {
			if (!enabled) {
				event.preventDefault();
				event.stopPropagation();
			}
		}

		private subMenus = [
			{ state: C.States.JobMetrics, collapsed: true },
			{ state: C.States.PitWizard, collapsed: true },
			{ state: C.States.PitConfig, collapsed: true }
		];

		private getSubMenu(state) {
			return _.find(this.subMenus, { state: state });
		}

		public IsCollapsed(state): boolean {
			return this.getSubMenu(state).collapsed;
		}

		public OnSubClick(event: ng.IAngularEvent, state, enabled) {
			console.log('OnSubClick', state, enabled);
			event.preventDefault();
			if (enabled) {
				this.subMenus.forEach(item => {
					if (item.state === state) {
						item.collapsed = !item.collapsed;
					} else {
						item.collapsed = true;
					}
				});
			}
		}

		public get SelectPitPrompt(): string {
			return _.isUndefined(this.pit) ? "Select a Pit" : this.pit.name;
		}

		public get FaultCount(): any {
			var count = 0;
			if (this.job) {
				count = this.job.faultCount;
			}
			return count || '';
		}

		public get JobRunningTooltip(): string {
			if (!this.CanSelectPit) {
				return "Disabled while running a Job or a Test";
			}
			return "";
		}

		public get FaultsUnavailableTooltip(): string {
			if (_.isUndefined(this.job)) {
				return "No Job available";
			}
			return "";
		}

		public get MetricsUnavailableTooltip(): string {
			if (_.isUndefined(this.job)) {
				return "No Job available";
			}
			return "";
		}

		public IsComplete(step: string) {
			return this.wizardService.GetTrack(step).isComplete;
		}

		public get CanSelectPit(): boolean {
			return !this.testService.IsPending
				&& !this.jobService.IsRunning
				&& !this.jobService.IsPaused;
		}

		public get CanConfigurePit(): boolean {
			return (
				(_.isUndefined(this.job) || this.job.status === JobStatus.Stopped) &&
				(!_.isUndefined(this.pit) && !_.isEmpty(this.pit.pitUrl) && this.pit.pitUrl.length > 0)
			);
		}

		public get CanViewFaults(): boolean {
			return !_.isUndefined(this.job);
		}

		public get CanViewMetrics(): boolean {
			return !_.isUndefined(this.job);
		}

		public OnSelectPit() {
			var modal = this.$modal.open({
				templateUrl: C.Templates.Modal.PitLibrary,
				controller: PitLibraryController
			});
			modal.result.then(() => {
				if (!this.pitService.IsConfigured) {
					this.$state.go(C.States.PitWizard, { track: C.Tracks.Intro });
				} else {
					this.$state.go(C.States.MainHome);
				}
			});
		}

		private showSidebar: boolean = false;
		private isMenuMin: boolean = false;

		public get IsMenuMinimized(): boolean {
			return this.isMenuMin;
		}

		public OnToggleSidebar() {
			this.isMenuMin = !this.isMenuMin;
		}

		public get SidebarClass() {
			return {
				'menu-min': this.IsMenuMinimized,
				'display': this.showSidebar
			};
		}

		public get MenuTogglerClass() {
			return {
				'display': this.showSidebar
			};
		}

		public OnMenuToggle() {
			this.showSidebar = !this.showSidebar;
		}

		public ShowMenu(name: string) {
			return this.$state.includes(name);
		}
	}
}
