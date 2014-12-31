/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class WizardController {
		public Question: IQuestion;
		public Step: number;

		static $inject = [
			"$scope",
			"$location",
			"PitService",
			"WizardService",
			"JobService"
		];

		constructor(
			private $scope: IFormScope,
			private $location: ng.ILocationService,
			private pitService: PitService,
			private wizardService: WizardService,
			private jobService: JobService
		) {
			$scope.vm = this;
			this.resetPrompts();
			var promise = this.track.Begin();
			if (promise) {
				promise.then(() => {
					this.Next();
				});
			}
		}

		public get IncludeUrl(): string {
			if (_.isUndefined(this.Question)) {
				return "";
			}
			if (_.isUndefined(this.Question.qref)) {
				var types = [
					'hwaddr',
					'iface',
					'ipv4',
					'ipv6'
				];
				var template = this.Question.type;
				if (_.contains(types, template)) {
					template = 'combo';
				}
				if (template === 'user') {
					template = 'string';
				}
				return 'html/q/' + template + '.html';
			}
			return this.Question.qref;
		}

		public get Defines(): IParameter[] {
			return this.pitService.PitConfig.config;
		}

		public get FaultAgentDescription(): string {
			var agent = this.wizardService.GetTrack("fault").agents[0];
			var ret = '';
			agent.monitors.forEach((item: IMonitor) => {
				if (!ret) {
					ret += ' ';
				}
				ret += item.description;
			});
			return ret;
		}

		public SelectedOptions = [];

		public get DataAgents(): Agent[] {
			return this.wizardService.GetTrack("data").agents;
		}

		public get AutoAgents(): Agent[] {
			return this.wizardService.GetTrack("auto").agents;
		}

		public get Title(): string {
			return this.track.title;
		}

		public get IsTestComplete(): boolean {
			return this.wizardService.GetTrack("test").isComplete;
		}

		public get IsSetVars(): boolean {
			return this.$location.path() === '/quickstart/setvars';
		}

		public NextPrompt: string;

		public get CanMoveNext(): boolean {
			return !this.$scope.form.$invalid;
		}

		public BackPrompt: string;

		public get CanMoveBack(): boolean {
			return this.track.history.length > 0;
		}

		public Next() {
			if (_.isUndefined(this.Question)) {
				// if we're not on a question, get the first available
				this.Question = this.track.GetQuestionById(0);
			} else {
				var q = this.Question;

				if (q.type === QuestionTypes.Done) {
					this.OnNextTrack();
					return;
				}

				if (q.type !== QuestionTypes.Jump) {
					this.track.history.push(q.id);
				}

				var nextId = this.getNextId(q);
				if (_.isUndefined(nextId)) {
					// no more questions, this track is complete
					this.track.Finish();

					this.Question = {
						id: -1,
						type: QuestionTypes.Done,
						qref: this.track.qref
					};

					this.NextPrompt = this.track.nextPrompt;
					this.BackPrompt = this.track.backPrompt;
				} else {
					this.Question = this.track.GetQuestionById(nextId);
				}
			}

			this.Step = 2;

			this.prepareQuestion();
		}

		public Back() {
			if (this.Question.type === QuestionTypes.Done) {
				this.OnRestart();
				return;
			}

			// pop the history and get the question
			var previousId = 0;
			if (this.track.history.length > 0) {
				previousId = this.track.history.pop();
			}

			this.Question = this.track.GetQuestionById(previousId);

			if (this.Question.type === QuestionTypes.Intro) {
				this.Step = 1;
			} else if (this.Question.type === QuestionTypes.Done) {
				this.Step = 3;
			} else {
				this.Step = 2;
			}
		}

		public OnNextTrack() {
			this.$location.path(this.track.next);
		}

		public OnRestart() {
			this.track.Restart();
			this.resetPrompts();
			this.Question = undefined;
			this.Next();
		}

		public OnRemoveAgent(index: number) {
			this.track.agents.splice(index, 1);
		}

		public get CanStartJob() {
			return this.jobService.CanStart;
		}

		public StartJob() {
			this.jobService.StartJob();
			this.OnNextTrack();
		}

		public OnInsertDefine(def: IParameter) {
			if (_.isUndefined(this.Question.value)) {
				this.Question.value = "";
			}
			this.Question.value += "##" + def.key + "##";
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
			// special stuff to do based on the next question
			switch (this.Question.type) {
				case QuestionTypes.Intro:
					this.Step = 1;
					break;
				case QuestionTypes.Jump:
					this.Next();
					break;
				case QuestionTypes.Choice:
					// what to do when there's no value already set for a choice question
					// if the first choice has a value, set default selection to first
					// if the first choice doesn't have a value it's a un-keyed choice, set default to 0
					// look at q-choice.html to see how its binding works
					if (_.isUndefined(this.Question.value)) {
						var first = _.first(this.Question.choice);
						if (_.isUndefined(first.value)) {
							this.Question.value = first.next;
						} else {
							this.Question.value = first.value;
						}
					}
					break;
				case QuestionTypes.Range:
					if (_.isUndefined(this.Question.value)) {
						this.Question.value = this.Question.rangeMin;
					}
					break;
				case QuestionTypes.Done:
					this.Step = 3;
					break;
				default:
					break;
			}
		}

		private get track(): ITrack {
			var re = new RegExp("/quickstart/([^/]+)");
			var match = re.exec(this.$location.path());
			if (match) {
				return this.wizardService.GetTrack(match[1]);
			}
			return this.wizardService.GetTrack("null");;
		}

		private resetPrompts() {
			this.NextPrompt = "Next";
			this.BackPrompt = "Back";
		}
	}
}
