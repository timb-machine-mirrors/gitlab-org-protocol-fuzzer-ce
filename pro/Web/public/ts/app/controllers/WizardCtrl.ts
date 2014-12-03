/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class WizardController {
		public Question: Models.IQuestion;
		public Step: number;

		static $inject = [
			"$scope",
			"$location",
			"PitService",
			"WizardService",
			"JobService"
		];

		constructor(
			private $scope: IViewModelScope,
			private $location: ng.ILocationService,
			private pitService: Services.PitService,
			private wizardService: Services.WizardService,
			private jobService: Services.JobService
		) {
			$scope.vm = this;
			var promise = this.track.Begin();
			if (promise) {
				promise.then(() => {
					this.Next();
				});
			}
		}

		public dataGridOptions: ngGrid.IGridOptions = {
			data: "vm.DataAgents",
			columnDefs: [
				{ cellTemplate: "html/grid/agents/cell.html", width: 40, maxWidth: 40 },
				{ cellTemplate: "html/grid/agents/agents.html" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public autoGridOptions: ngGrid.IGridOptions = {
			data: "vm.AutoAgents",
			columnDefs: [
				{ cellTemplate: "html/grid/agents/cell.html", width: 40, maxWidth: 40 },
				{ cellTemplate: "html/grid/agents/agents.html" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public definesGridOptions: ngGrid.IGridOptions = {
			data: "vm.Defines",
			columnDefs: [
				{ field: "name", displayName: "Name" },
				{ field: "key", displayName: "Key" },
				{ field: "value", displayName: "Value" },
				{ cellTemplate: "html/grid/vars/cell.html" }
			],
			enableCellSelection: false,
			enableRowSelection: false,
			multiSelect: false,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public gridReview: ngGrid.IGridOptions = {
			data: "vm.Defines",
			columnDefs: [
				{ field: "name", displayName: "Name" },
				{ field: "key", displayName: "Key" },
				{ field: "value", displayName: "Value" }
			],
			enableCellSelection: false,
			enableRowSelection: false,
			multiSelect: false,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public get Defines(): Models.IParameter[] {
			return this.pitService.PitConfig.config;
		}

		public get FaultAgentDescription(): string {
			var agent = this.wizardService.GetTrack("fault").agents[0];
			var ret = '';
			agent.monitors.forEach((item: Models.IMonitor) => {
				if (!ret) {
					ret += ' ';
				}
				ret += item.description;
			});
			return ret;
		}

		public get DataAgents(): Models.Agent[] {
			return this.wizardService.GetTrack("data").agents;
		}

		public get AutoAgents(): Models.Agent[] {
			return this.wizardService.GetTrack("auto").agents;
		}

		public get Title(): string {
			return this.track.title;
		}

		public get IsTestComplete(): boolean {
			return this.wizardService.GetTrack("test").isComplete;
		}

		private getNextId(q: Models.IQuestion): number {
			if (q.type === Models.QuestionTypes.Choice ||
				q.type === Models.QuestionTypes.Jump) {
				// get next id from selected choice
				var choice = _.find(q.choice, e => {
					if (e.value === undefined && e.next !== undefined) {
						return e.next.toString() === q.value.toString();
					} else if (e.value !== undefined && q.value !== undefined) {
						return e.value.toString() === q.value.toString();
					} else {
						return false;
					}
				});

				if (_.isObject(choice)) {
					return choice.next;
				}

				if (q.value !== undefined) {
					return q.choice[parseInt(q.value)].next;
				}

				return undefined;
			}
			return q.next;
		}

		public Next() {
			if (this.Question === undefined) {
				// if we're not on a question, get the first available
				this.Question = this.track.GetQuestionById(0);
			} else {
				var q = this.Question;
				if (q.type !== Models.QuestionTypes.Jump) {
					this.track.history.push(q.id);
				}

				var nextId = this.getNextId(q);
				if (nextId === undefined) {
					// no more questions, this track is complete
					this.track.Finish();

					this.Question = {
						id: -1,
						type: Models.QuestionTypes.Done,
						qref: this.track.qref
					};
				} else {
					this.Question = this.track.GetQuestionById(nextId);
				}
			}

			this.Step = 2;

			this.prepareQuestion();
		}

		private prepareQuestion() {
			// special stuff to do based on the next question
			switch (this.Question.type) {
				case Models.QuestionTypes.Intro:
					this.Step = 1;
					break;
				case Models.QuestionTypes.Jump:
					this.Next();
					break;
				case Models.QuestionTypes.Choice:
					// what to do when there's no value already set for a choice question
					// if the first choice has a value, set default selection to first
					// if the first choice doesn't have a value it's a un-keyed choice, set default to 0
					// look at q-choice.html to see how its binding works
					if (this.Question.value === undefined) {
						if (this.Question.choice[0].value === undefined) {
							this.Question.value = this.Question.choice[0].next;
						} else {
							this.Question.value = this.Question.choice[0].value;
						}
					}
					break;
				case Models.QuestionTypes.Range:
					if (this.Question.value === undefined) {
						this.Question.value = this.Question.rangeMin;
					}
					break;
				case Models.QuestionTypes.Done:
					this.Step = 3;
					break;
				default:
					break;
			}
		}

		public Back() {
			// pop the history and get the question
			var previousId = 0;
			if (this.track.history.length > 0) {
				previousId = this.track.history.pop();
			}

			this.Question = this.track.GetQuestionById(previousId);

			if (this.Question.type === Models.QuestionTypes.Intro) {
				this.Step = 1;
			} else if (this.Question.type === Models.QuestionTypes.Done) {
				this.Step = 3;
			} else {
				this.Step = 2;
			}
		}

		public OnSubmit() {
			this.track.isComplete = true;
			this.$location.path(this.track.next);
		}

		public OnRestart() {
			this.track.Restart();
			this.Question = undefined;
			this.Next();
		}

		public OnRemoveMonitor(index: number) {
			this.track.agents.splice(index, 1);
		}

		public get CanStartJob() {
			return this.jobService.CanStartJob;
		}

		public StartJob() {
			this.jobService.StartJob();
			this.OnSubmit();
		}

		public OnInsertDefine(row) {
			if (this.Question.value === undefined) {
				this.Question.value = "";
			}
			this.Question.value += "##" + row.getProperty("key") + "##";
		}

		private get track(): Models.ITrack {
			var re = new RegExp("/quickstart/([^/]+)");
			var match = re.exec(this.$location.path());
			if (match) {
				return this.wizardService.GetTrack(match[1]);
			}
			return this.wizardService.GetTrack("null");;
		}
	}
}
