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
			"$scope",
			"$routeParams",
			"$http",
			"visDataSet",
			"JobService"
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

		public MetricsFaultsOverTimeData: LinearChartData = {
			labels: [],
			datasets: []
		};

		public MetricsFaultsOverTime: ITimelineOptions = {
			responsive: true,
			showTooltips: true,
			tooltipEvents: ["mousemove", "touchstart", "touchmove"],
			tooltipFillColor: "rgba(0,0,0,0.8)",
			tooltipFontFamily: "'Helvetica Neue', 'Helvetica', 'Arial', sans-serif",
			tooltipFontSize: 14,
			tooltipFontStyle: "normal",
			tooltipFontColor: "#fff",
			tooltipTitleFontFamily: "'Helvetica Neue', 'Helvetica', 'Arial', sans-serif",
			tooltipTitleFontSize: 14,
			tooltipTitleFontStyle: "bold",
			tooltipTitleFontColor: "#fff",
			tooltipYPadding: 6,
			tooltipXPadding: 6,
			tooltipCaretSize: 8,
			tooltipCornerRadius: 6,
			tooltipXOffset: 10,
			tooltipTemplate: "<%if (label){%><%=label%>: <%}%><%= value %>",
			multiTooltipTemplate: "<%= value %>",
			showScale: true,
			scaleOverride: false,
			scaleStartValue: 0,
			scaleShowGridLines: true,
			scaleGridLineColor: "rgba(0,0,0,.05)",
			scaleGridLineWidth: 1,
			bezierCurve: true,
			bezierCurveTension: 0.4,
			pointDot: true,
			pointDotRadius: 4,
			pointDotStrokeWidth: 1,
			pointHitDetectionRadius: 20,
			datasetStroke: true,
			datasetStrokeWidth: 2,
			datasetFill: true
		};

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
			case "bucketTimeline":
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
			case "faultTimeline":
				promise.success((data: IFaultTimelineMetric[]) => {
					this.MetricsFaultsOverTimeData = {
						labels: data.map(i => moment(i.date).format("M/D h a")),
						datasets: [
							{
								label: "My First dataset",
								fillColor: "rgba(0,0,220,0.2)",
								strokeColor: "rgba(220,220,220,1)",
								pointColor: "rgba(220,220,220,1)",
								pointStrokeColor: "#fff",
								pointHighlightFill: "#fff",
								pointHighlightStroke: "rgba(220,220,220,1)",
								data: data.map(i => i.faultCount)
							}
						]
					};
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
