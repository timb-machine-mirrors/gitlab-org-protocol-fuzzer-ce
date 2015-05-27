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
			C.Services.Wizard
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private $modal: ng.ui.bootstrap.IModalService,
			private jobService: JobService,
			private testService: TestService,
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
		}

		private showSidebar: boolean = false;
		private isMenuMin: boolean = false;

		private get job(): IJob {
			return this.jobService.Job;
		}

		public get JobId(): number {
			return this.$state.params['job'];
		}

		public get PitId(): number {
			return this.$state.params['pit'];
		}

		public Metrics = C.MetricsList;

		public WizardTracks = Peach.WizardTracks;

		public ConfigSteps = [
			{ id: C.States.PitAdvancedVariables, name: 'Variables' },
			{ id: C.States.PitAdvancedMonitoring, name: 'Monitoring' },
			{ id: C.States.PitAdvancedTest, name: 'Test' }
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
			{ state: C.States.PitAdvanced, collapsed: true }
		];

		public IsCollapsed(state): boolean {
			var subMenu = _.find(this.subMenus, { state: state });
			return subMenu.collapsed;
		}

		public OnSubClick(event: ng.IAngularEvent, state) {
			event.preventDefault();
			this.subMenus.forEach(item => {
				if (item.state === state) {
					item.collapsed = !item.collapsed;
				} else {
					item.collapsed = true;
				}
			});
		}

		public get FaultCount(): any {
			var count = 0;
			if (this.job) {
				count = this.job.faultCount;
			}
			return count || '';
		}

		public IsComplete(step: string): boolean {
			return this.wizardService.GetTrack(step).isComplete;
		}

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

		public ShowMenu(name: string): boolean {
			return this.$state.includes(name);
		}

		public MetricUrl(metric: C.IMetric): string {
			var state = C.States.JobMetrics + '.' + metric.id;
			var params = { job: this.JobId };
			return this.$state.href(state, params);
		}
	}
}
