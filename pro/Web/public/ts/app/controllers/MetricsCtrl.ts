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
		data?: Models.IBucketTimelineMetric
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
			private jobService: Services.JobService
		) {
			$scope.vm = this;
			this.metric = $routeParams['metric'];
			this.initializeData();
		}

		public get IncludeUrl(): string {
			return 'html/metrics/' + this.metric + '.html';
		}

		private metric: string;

		public MutatorData: Models.IMutatorMetric[] = [];
		public ElementData: Models.IElementMetric[] = [];
		public DatasetData: Models.IDatasetMetric[] = [];
		public StateData: Models.IStateMetric[] = [];
		public BucketData: Models.IBucketMetric[] = [];

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

		public GridMetricsMutator: ngGrid.IGridOptions = {
			data: "vm.MutatorData",
			sortInfo: { fields: ["mutator"], directions: ["asc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "mutator", displayName: "Mutator" },
				{ field: "elementCount", displayName: "Element Count" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "bucketCount", displayName: "Bucket Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			],
			plugins: [ new ngGridFlexibleHeightPlugin() ]
		};

		public GridMetricsElement: ngGrid.IGridOptions = {
			data: "vm.ElementData",
			sortInfo: { fields: ["faultCount"], directions: ["desc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "state", displayName: "State" },
				{ field: "action", displayName: "Action" },
				{ field: "parameter", displayName: "Parameter" },
				{ field: "element", displayName: "Element" },
				{ field: "iterationCount", displayName: "Iterations" },
				{ field: "bucketCount", displayName: "Buckets" },
				{ field: "faultCount", displayName: "Faults" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public GridMetricsState: ngGrid.IGridOptions = {
			data: "vm.StateData",
			sortInfo: { fields: ["state"], directions: ["asc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "state", displayName: "State" },
				{ field: "executionCount", displayName: "Executions" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public GridMetricsDataset: ngGrid.IGridOptions = {
			data: "vm.DatasetData",
			sortInfo: { fields: ["faultCount"], directions: ["desc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "dataset", displayName: "Data Set" },
				{ field: "iterationCount", displayName: "Iterations" },
				{ field: "bucketCount", displayName: "Buckets" },
				{ field: "faultCount", displayName: "Faults" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public GridMetricsBucket: ngGrid.IGridOptions = {
			data: "vm.BucketData",
			sortInfo: { fields: ["faultCount"], directions: ["desc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "bucket", displayName: "Fault Bucket" },
				{ field: "mutator", displayName: "Mutator" },
				{ field: "element", displayName: "Element" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			],
			plugins: [new ngGridFlexibleHeightPlugin()]
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
				promise.success((data: Models.IBucketTimelineMetric[]) => {
					var items = data.map((item: Models.IBucketTimelineMetric) => {
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
				promise.success((data: Models.IFaultTimelineMetric[]) => {
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
				promise.success((data: Models.IMutatorMetric[]) => {
					this.MutatorData = data;
				});
				break;
			case "elements":
				promise.success((data: Models.IElementMetric[]) => {
					this.ElementData = data;
				});
				break;
			case "dataset":
				promise.success((data: Models.IDatasetMetric[]) => {
					this.DatasetData = data;
				});
				break;
			case "states":
				promise.success((data: Models.IStateMetric[]) => {
					this.StateData = data;
				});
				break;
			case "buckets":
				promise.success((data: Models.IBucketMetric[]) => {
					this.BucketData = data;
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
