/// <reference path="../reference.ts" />

namespace Peach {
	enum WizardStep {
		Intro = 1,
		QandA,
		Review
	}

	export interface IWizardScope extends IFormScope {
		Title: string;
		Question: IQuestion;
		Step: WizardStep;
		NextPrompt: string;
		BackPrompt: string;
		Defines: IParameter[];
	}

	export class WizardController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Pit,
			C.Services.Wizard
		];

		constructor(
			private $scope: IWizardScope,
			private $state: ng.ui.IStateService,
			private pitService: PitService,
			private wizardService: WizardService
		) {
			const trackId = this.$state.current.data.track;
			this.track = wizardService.GetTrack(trackId);
			this.$scope.Title = this.track.name;

			if (trackId !== C.Tracks.Intro) {
				this.unregister = this.$scope.$on(C.Angular.$stateChangeSuccess, () => {
					this.onStateChange();
				});
			}
		}

		private unregister;
		private track: ITrack;

		public OnNextTrack(): void {
			// this is needed to make unit tests happy
			if (this.unregister) { 
				this.unregister();
			}

			const next = this.track.next;
			this.$state.go(next.state, next.params);
		}

		private onStateChange(): void {
			this.resetPrompts();

			if (this.$state.is(this.track.start)) {
				this.initIntro();
			} else if (this.$state.is(this.track.finish)) {
				this.initReview();
			} else {
				const id = this.$state.params['id'];
				this.$scope.Step = WizardStep.QandA;
				this.loadQuestion(id);
			}
		}

		private initIntro(): void {
			this.$scope.Step = WizardStep.Intro;
			this.$scope.Question = {
				id: 0,
				type: QuestionTypes.Intro
			};
		}

		private initReview(): void {
			if (!this.track.IsValid()) {
				this.$state.go(this.track.start);
				return;
			}
			this.$scope.Step = WizardStep.Review;
			this.track.Finish();
			this.$scope.Question = {
				id: -1,
				type: QuestionTypes.Done
			};

			// TODO: use WizardView
			this.$scope.Defines = this.pitService.Pit.config;
			this.$scope.NextPrompt = this.track.nextPrompt;
			this.$scope.BackPrompt = this.track.backPrompt;
		}

		private loadQuestion(id: number): void {
			this.$scope.Question = this.track.GetQuestionById(id);
			if (this.$scope.Question) {
				this.prepareQuestion();
			} else {
				this.$state.go(this.track.start);
			}
		}

		public get FaultAgentDescription(): string {
			const agent = this.wizardService.GetTrack(C.Tracks.Fault).agents[0];
			let ret = '';
			agent.monitors.forEach((item: IMonitor) => {
				if (!ret) {
					ret += ' ';
				}
				ret += item.description;
			});
			return ret;
		}

		public get Agents(): IAgent[] {
			return this.track.agents;
		}

		public get IsTestComplete(): boolean {
			return this.wizardService.GetTrack(C.Tracks.Test).isComplete;
		}

		public get CanMoveNext(): boolean {
			return this.$scope.Question && !this.$scope.form.$invalid;
		}

		public get CanMoveBack(): boolean {
			return this.track.history.length > 0;
		}

		public Next(): void {
			if (this.$scope.Question.type === QuestionTypes.Done) {
				this.OnNextTrack();
				return;
			}

			if (this.$scope.Question.type === QuestionTypes.Intro) {
				const promise = this.track.Begin();
				if (promise) {
					promise.then(() => {
						const first = this.track.GetQuestionById(1);
						if (_.isUndefined(first)) {
							this.$state.go(this.track.finish);
						} else {
							this.$state.go(this.track.steps, { id: 1 });
						}
					});
				}
				return;
			}

			if (this.$scope.Question.type !== QuestionTypes.Jump) {
				this.track.history.push(this.$scope.Question.id);
			}

			const nextId = this.getNextId(this.$scope.Question);

			if (_.isUndefined(nextId)) {
				// no more questions, this track is complete
				this.$state.go(this.track.finish);
			} else {
				this.$state.go(this.track.steps, { id: nextId });
			}
		}

		public Back(): void {
			if (this.$scope.Question.type === QuestionTypes.Done) {
				this.OnRestart();
				return;
			}

			let previousId = 0;
			if (this.track.history.length > 0) {
				previousId = this.track.history.pop();
			}

			if (previousId === 0) {
				this.$state.go(this.track.start);
			} else {
				this.$state.go(this.track.steps, { id: previousId });
			}
		}

		public OnRestart(): void {
			this.track.history = [];
			this.$state.go(this.track.start);
		}

		public OnRemoveAgent(index: number): void {
			this.track.agents.splice(index, 1);
		}

		private getNextId(q: IQuestion): number {
			if (q.type === QuestionTypes.Choice ||
				q.type === QuestionTypes.Jump) {
				// get next id from selected choice
				const choice = _.find(q.choice, e => {
					if (_.isUndefined(e.value) && !_.isUndefined(e.next)) {
						return e.next.toString() === q.value.toString();
					} else if (!_.isUndefined(e.value) && !_.isUndefined(q.value)) {
						return e.value.toString() === q.value.toString();
					} else {
						return false;
					}
				});

				if (_.isObject(choice)) {
					return choice.next;
				}

				if (!_.isUndefined(q.value)) {
					return q.choice[parseInt(q.value)].next;
				}

				return undefined;
			}
			return q.next;
		}

		private prepareQuestion(): void {
			// TODO: use WizardView
			this.$scope.Defines = this.pitService.Pit.config;

			// special stuff to do based on the next question
			switch (this.$scope.Question.type) {
				case QuestionTypes.Jump:
					this.Next();
					break;
				case QuestionTypes.Choice:
					// what to do when there's no value already set for a choice question
					// if the first choice has a value, set default selection to first
					// if the first choice doesn't have a value it's a un-keyed choice, set default to 0
					// look at q-choice.html to see how its binding works
					if (_.isUndefined(this.$scope.Question.value)) {
						const first = _.first(this.$scope.Question.choice);
						if (_.isUndefined(first.value)) {
							this.$scope.Question.value = first.next;
						} else {
							this.$scope.Question.value = first.value;
						}
					}
					break;
				case QuestionTypes.Range:
					if (_.isUndefined(this.$scope.Question.value)) {
						this.$scope.Question.value = this.$scope.Question.rangeMin;
					}
					break;
				default:
					break;
			}
		}

		private resetPrompts(): void {
			this.$scope.NextPrompt = "Next";
			this.$scope.BackPrompt = "Back";
		}
	}

	export class WizardQuestionController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state
		];

		constructor(
			private $scope: IWizardScope,
			private $state: ng.ui.IStateService
		) {
		}

		public get IncludeUrl(): string {
			if (_.isUndefined(this.$scope.Question)) {
				return "";
			}
			const types = [
				QuestionTypes.Hwaddr,
				QuestionTypes.Iface,
				QuestionTypes.Ipv4,
				QuestionTypes.Ipv6
			];
			let template = this.$scope.Question.type;
			if (_.includes(types, template)) {
				template = QuestionTypes.Combo;
			}
			if (template === QuestionTypes.User ||
				template === QuestionTypes.Hex) {
				template = QuestionTypes.String;
			}
			if (template === QuestionTypes.Bool) {
				template = QuestionTypes.Enum;
			}
			return C.Templates.Pit.Wizard.QuestionType.replace(':type', template);
		}

		public get IsSetVars(): boolean {
			return this.$state.includes(WizardTrackIntro(C.Tracks.Vars));
		}

		public OnInsertDefine(def: IParameter) {
			if (_.isUndefined(this.$scope.Question.value)) {
				this.$scope.Question.value = "";
			}
			this.$scope.Question.value += `##${def.key}##`;
		}
	}
}
