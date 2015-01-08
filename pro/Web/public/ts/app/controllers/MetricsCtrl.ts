/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface ITimelineItem {
		className?: string;
		content: string;
		end?: Date;
		group?: any;
		id?: number;
		start: Date;
		title?: string;
		type?: string;
		data?: IBucketTimelineMetric
	}

	export interface ITimelineOptions {
		align?: string;
		autoResize?: boolean;
		clickToUse?: boolean;
		dataAttributes?: string[];
		editable?: any;
		end?: any;
		groupOrder?: any;
		height?: any;
		locale?: string;
		locales?: Object;
		margin?: Object;
		max?: any;
		maxHeight?: any;
		min?: any;
		minHeight?: any;
		onAdd?: Function;
		onUpdate?: Function;
		onMove?: Function;
		onMoving?: Function;
		onRemove?: Function;
		orientation?: string;
		padding?: number;
		selectable?: boolean;
		showCurrentTime?: boolean;
		showCustomTime?: boolean;
		showMajorLabels?: boolean;
		showMinorLabels?: boolean;
		stack?: boolean;
		start?: any;
		template?: Function;
		type?: string;
		width?: string;
		zoomable?: boolean;
		zoomMax?: number;
		zoomMin?: number;
	}

	export class MetricsController {
		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$routeParams,
			Constants.Angular.$http,
			"visDataSet",
			Constants.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			$routeParams: ng.route.IRouteParamsService,
			private $http: ng.IHttpService,
			private visDataSet,
			private jobService: JobService
		) {
			$scope.vm = this;
			this.metric = $routeParams['metric'];
			this.initializeData();
		}

		public get IncludeUrl(): string {
			return 'html/metrics/' + this.metric + '.html';
		}

		private metric: string;

		public MutatorData: IMutatorMetric[] = [];
		public AllMutatorData: IMutatorMetric[] = [];

		public ElementData: IElementMetric[] = [];
		public AllElementData: IElementMetric[] = [];

		public DatasetData: IDatasetMetric[] = [];
		public AllDatasetData: IDatasetMetric[] = [];

		public StateData: IStateMetric[] = [];
		public AllStateData: IStateMetric[] = [];

		public BucketData: IBucketMetric[] = [];
		public AllBucketData: IBucketMetric[] = [];

		public FaultsOverTimeLabels: string[] = [
			moment(Date.now()).format("M/D h a")
		];

		public FaultsOverTimeData: number[][] = [
			[0]
		];

		public BucketTimelineData = {
			single: true,
			load: []
		};

		public BucketTimelineOptions: ITimelineOptions = {
			showCurrentTime: true,
			selectable: false,
			type: "box",
			template: (item: ITimelineItem) => {
				if (item.content) {
					return item.content;
				}
				return "<div>" +
						"<a ng-click='event.stopPropagation()' " +
						"href='#/faults/" + item.data.label + "' " +
						"style='background: transparent'>" +
							item.data.label +
						"</a>" +
						"<br />" +
						"Faults: " + item.data.faultCount + "<br />" +
						"1st Iteration: " + item.data.iteration + "<br />" +
					"</div>";
			}
		}

		private initializeData(): void {
			var promise = this.getData();
			switch (this.metric) {
			case "bucket-timeline":
				promise.success((data: IBucketTimelineMetric[]) => {
					var items = data.map((item: IBucketTimelineMetric) => {
						return <ITimelineItem> {
							id: item.id,
							content: "",
							start: item.time,
							data: item
						};
					});
					items.unshift({
						id: 0,
						style: "color: green",
						content: "Job Start",
						start: this.jobService.Job.startDate
					});
					if (this.jobService.Job.stopDate) {
						items.push({
							id: -1,
							style: "color: red",
							content: "Job End",
							start: this.jobService.Job.stopDate
						});
					}
					this.BucketTimelineData = this.visDataSet(items);
				});
				break;
			case "fault-timeline":
				promise.success((data: IFaultTimelineMetric[]) => {
					if (data.length === 0) {
						this.FaultsOverTimeLabels = [moment(Date.now()).format("M/D h a")];
						this.FaultsOverTimeData = [[0]];
					} else {
						this.FaultsOverTimeLabels = data.map(x => moment(x.date).format("M/D h a")),
						this.FaultsOverTimeData = [_.pluck(data, 'faultCount')];
					}
				});
				break;
			case "mutators":
				this.MutatorData = _.clone(this.AllMutatorData);
				promise.success((data: IMutatorMetric[]) => {
					this.AllMutatorData = data;
				});
				break;
			case "elements":
				this.ElementData = _.clone(this.AllElementData);
				promise.success((data: IElementMetric[]) => {
					this.AllElementData = data;
				});
				break;
			case "dataset":
				this.DatasetData = _.clone(this.AllDatasetData);
				promise.success((data: IDatasetMetric[]) => {
					this.AllDatasetData = data;
				});
				break;
			case "states":
				this.StateData = _.clone(this.AllStateData);
				promise.success((data: IStateMetric[]) => {
					this.AllStateData = data;
				});
				break;
			case "buckets":
				this.BucketData = _.clone(this.AllBucketData);
				promise.success((data: IBucketMetric[]) => {
					this.AllBucketData = data;
				});
				break;
			}
		}

		private getData<T>(): ng.IHttpPromise<T> {
			return this.$http.get(this.makeUrl(this.metric));
		}

		private makeUrl(part: string): string {
			return this.jobService.Job.jobUrl + "/metrics/" + part;
		}
	}
}
