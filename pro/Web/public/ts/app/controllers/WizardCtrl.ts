/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IWizardScope extends IFormScope {
		Question: IQuestion;
		Step: number;
		NextPrompt: string;
		BackPrompt: string;
		Defines: IParameter[];
	}

	enum WizardStep {
		Intro = 1,
		QA,
		Review
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
			this.trackId = this.$state.params['track'];
			this.track = wizardService.GetTrack(this.trackId);
			this.Title = this.track.name;

			if (this.trackId !== C.Tracks.Intro) {
				var id = this.$state.params['id'];
				this.init(id);
			}
		}

		private trackId: string;
		private track: ITrack;
		public Title: string;

		public OnNextTrack() {
			var next = this.track.next;
			this.$state.go(next.state, next.params);
		}

		private gotoIntro() {
			// since the all the params might be the same,
			// we need to force the controller to reinitialize with reload: true.
			this.$state.go(
				C.States.PitWizardIntro,
				{ track: this.trackId, id: 0 },
				{ reload: true }
			);
		}

		private init(id: number) {
			this.resetPrompts();

			if (id === 0) {
				if (this.$state.is(C.States.PitWizardIntro)) {
					this.initIntro();
				}
				else if (this.$state.is(C.States.PitWizardReview)) {
					this.initReview();
				}
			} else {
				this.$scope.Step = WizardStep.QA;
				this.loadQuestion(id);
			}
		}

		private initIntro() {
			this.$scope.Step = WizardStep.Intro;
			var promise = this.track.Begin();
			if (promise) {
				promise.then(() => {
					this.loadQuestion(0);
				});
			}
		}

		private initReview() {
			if (!this.track.IsValid()) {
				this.gotoIntro();
				return;
			}
			this.$scope.Step = WizardStep.Review;
			this.track.Finish();
			this.$scope.Question = {
				id: -1,
				type: QuestionTypes.Done
			};

			this.$scope.Defines = this.pitService.Pit.config;
			this.$scope.NextPrompt = this.track.nextPrompt;
			this.$scope.BackPrompt = this.track.backPrompt;
		}

		private loadQuestion(id: number) {
			this.$scope.Question = this.track.GetQuestionById(id);
			if (this.$scope.Question) {
				this.prepareQuestion();
			} else {
				this.gotoIntro();
			}
		}

		public get FaultAgentDescription(): string {
			var agent = this.wizardService.GetTrack(C.Tracks.Fault).agents[0];
			var ret = '';
			agent.monitors.forEach((item: IMonitor) => {
				if (!ret) {
					ret += ' ';
				}
				ret += item.description;
			});
			return ret;
		}

		public get Agents(): Agent[] {
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

		public Next() {
			if (this.$scope.Question.type === QuestionTypes.Done) {
				this.OnNextTrack();
				return;
			}

			if (this.$scope.Question.type !== QuestionTypes.Jump) {
				this.track.history.push(this.$scope.Question.id);
			}

			var nextId = this.getNextId(this.$scope.Question);

			if (_.isUndefined(nextId)) {
				// no more questions, this track is complete
				this.$state.go(
					C.States.PitWizardReview,
					{ track: this.trackId, id: 0 }
				);
			} else {
				this.$state.go(
					C.States.PitWizardQuestion,
					{ track: this.trackId, id: nextId }
				);
			}
		}

		public Back() {
			if (this.$scope.Question.type === QuestionTypes.Done) {
				this.OnRestart();
				return;
			}

			var previousId = 0;
			if (this.track.history.length > 0) {
				previousId = this.track.history.pop();
			}

			if (previousId === 0) {
				this.gotoIntro();
			} else {
				this.$state.go(
					C.States.PitWizardQuestion,
					{ track: this.trackId, id: previousId }
				);
			}
		}

		public OnRestart() {
			this.track.Restart();
			this.gotoIntro();
		}

		public OnRemoveAgent(index: number) {
			this.track.agents.splice(index, 1);
		}

		private getNextId(q: IQuestion): number {
			if (q.type === QuestionTypes.Choice ||
				q.type === QuestionTypes.Jump) {
				// get next id from selected choice
				var choice = _.find(q.choice, e => {
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

		private prepareQuestion() {
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
						var first = _.first(this.$scope.Question.choice);
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

		private resetPrompts() {
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
			var types = [
				QuestionTypes.HwAddress,
				QuestionTypes.Iface,
				QuestionTypes.Ipv4,
				QuestionTypes.Ipv6
			];
			var template = this.$scope.Question.type;
			if (_.contains(types, template)) {
				template = 'combo';
			}
			if (template === 'user') {
				template = 'string';
			}
			return C.Templates.Pit.Wizard.QuestionType.replace(':type', template);
		}

		public get IsSetVars(): boolean {
			return this.$state.includes(
				C.States.PitWizard,
				{ track: C.Tracks.Vars }
			);
		}

		public OnInsertDefine(def: IParameter) {
			if (_.isUndefined(this.$scope.Question.value)) {
				this.$scope.Question.value = "";
			}
			this.$scope.Question.value += "##" + def.key + "##";
		}
	}
}
