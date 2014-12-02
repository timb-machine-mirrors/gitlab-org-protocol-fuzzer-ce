/// <reference path="../reference.ts" />

/*
module Peach {
	"use strict";

	export interface MetricsParams extends ng.route.IRouteParamsService {
		metric: string;
	}

	export interface TimelineItem {
		className?: string;
		content: string;
		end?: Date;
		group?: any;
		id?: number;
		start: Date;
		title?: string;
		type: string;
		data?: Models.IBucketTimelineMetric
	}

	export interface TimelineOptions {
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
		private scope: ViewModelScope;

		public metric: string;

		//public bucketTimelineData: Models.BucketTimelineMetric[] = [];
		public bucketTimelineData = {};

		public mutatorData: Models.IMutatorMetric[] = [];
		public elementData: Models.IElementMetric[] = [];
		public datasetData: Models.IDatasetMetric[] = [];
		public stateData: Models.IStateMetric[] = [];
		public bucketData: Models.IBucketMetric[] = [];

		static $inject = [
			"$scope",
			"$routeParams",
			"pitConfiguratorService",
			"peachService",
			"visDataSet"
		];

		constructor(
			$scope: ViewModelScope,
			$routeParams: MetricsParams,
			private pitConfigSvc: Services.PitConfiguratorService,
			private peachSvc: Services.PeachService,
			private visDataSet
		) {
			$scope.vm = this;
			this.scope = $scope;
			this.metric = $routeParams.metric;
			this.initializeData();
		}

		public bucketTimelineOptions: TimelineOptions = {
			selectable: false,
			template: function (item: TimelineItem) {
				return "<div><a ng-click=\"event.stopPropagation()\" href=\"#/faults/" + item.data.label + "\" style=\"background: transparent\">" + item.data.label + "</a><br />" +
					"Faults: " + item.data.faultCount + "<br />" +
					"1st Iteration: " + item.data.iteration + "<br /></div>";
			}
		};

		private faultTimelineData: Models.IFaultTimelineMetric[] = [];

		public metrics_faultsOverTime_chart = {
			labels:	[]
		};

		public metrics_faultsOverTime_options = {};

		public gridMetricsMutator = {
			data: "vm.mutatorData",
			sortInfo: { fields: ["mutator"], directions: ["asc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "mutator", displayName: "Mutator" },
				{ field: "elementCount", displayName: "Element Count" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "bucketCount", displayName: "Bucket Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			]
		};

		public gridMetricsElement = {
			data: "vm.elementData",
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
			]
		};

		public gridMetricsState = {
			data: "vm.stateData",
			sortInfo: { fields: ["state"], directions: ["asc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "state", displayName: "State" },
				{ field: "executionCount", displayName: "Executions" }
			]
		};

		public gridMetricsDataset = {
			data: "vm.datasetData",
			sortInfo: { fields: ["faultCount"], directions: ["desc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "dataset", displayName: "Data Set" },
				{ field: "iterationCount", displayName: "Iterations" },
				{ field: "bucketCount", displayName: "Buckets" },
				{ field: "faultCount", displayName: "Faults" }
			]
		};

		public gridMetricsBucket = {
			data: "vm.bucketData",
			sortInfo: { fields: ["faultCount"], directions: ["desc"] },
			enableColumnResize: true,
			columnDefs: [
				{ field: "bucket", displayName: "Fault Bucket" },
				{ field: "mutator", displayName: "Mutator" },
				{ field: "element", displayName: "Element" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			]
		};

		private initializeData(): void {
			this.peachSvc.GetJobs((job: Models.IJob) => {
				var jobUrl = job.jobUrl;
				var jobStart = job.startDate;

				switch (this.metric) {
					case "bucketTimeline":
						this.peachSvc.GetBucketTimeline(jobUrl, (data: Models.IBucketTimelineMetric[]) => {
							var timelineData = [];
							data.forEach((item: Models.IBucketTimelineMetric) => {
								timelineData.push({
									id: item.id,
									type: "box",
									content: "",
									data: item,
									start: item.time
								});
							});
							var dataset = this.visDataSet(timelineData);
							this.bucketTimelineData = dataset;
							this.bucketTimelineOptions = { selectable: false };
						});
						break;
					case "faultsOverTime":
						this.peachSvc.GetFaultTimeline(jobUrl, (data: Models.IFaultTimelineMetric[]) => {
							this.faultTimelineData = data;

							this.metrics_faultsOverTime_chart = {
								labels: this.faultTimelineData.map(i => moment(i.date).format("M/D h a")),
								datasets: [
									{
										label: "My First dataset",
										fillColor: "rgba(0,0,220,0.2)",
										strokeColor: "rgba(220,220,220,1)",
										pointColor: "rgba(220,220,220,1)",
										pointStrokeColor: "#fff",
										pointHighlightFill: "#fff",
										pointHighlightStroke: "rgba(220,220,220,1)",
										data: this.faultTimelineData.map(i => i.faultCount)
									}
								]
							};

							this.metrics_faultsOverTime_options = {
								// Boolean - whether or not the chart should be responsive and resize when the browser does.
								responsive: true,

								// Boolean - Determines whether to draw tooltips on the canvas or not
								showTooltips: true,

								// Array - Array of string names to attach tooltip events
								tooltipEvents: ["mousemove", "touchstart", "touchmove"],

								// String - Tooltip background colour
								tooltipFillColor: "rgba(0,0,0,0.8)",

								// String - Tooltip label font declaration for the scale label
								tooltipFontFamily: "'Helvetica Neue', 'Helvetica', 'Arial', sans-serif",

								// Number - Tooltip label font size in pixels
								tooltipFontSize: 14,

								// String - Tooltip font weight style
								tooltipFontStyle: "normal",

								// String - Tooltip label font colour
								tooltipFontColor: "#fff",

								// String - Tooltip title font declaration for the scale label
								tooltipTitleFontFamily: "'Helvetica Neue', 'Helvetica', 'Arial', sans-serif",

								// Number - Tooltip title font size in pixels
								tooltipTitleFontSize: 14,

								// String - Tooltip title font weight style
								tooltipTitleFontStyle: "bold",

								// String - Tooltip title font colour
								tooltipTitleFontColor: "#fff",

								// Number - pixel width of padding around tooltip text
								tooltipYPadding: 6,

								// Number - pixel width of padding around tooltip text
								tooltipXPadding: 6,

								// Number - Size of the caret on the tooltip
								tooltipCaretSize: 8,

								// Number - Pixel radius of the tooltip border
								tooltipCornerRadius: 6,

								// Number - Pixel offset from point x to tooltip edge
								tooltipXOffset: 10,

								// String - Template string for single tooltips
								tooltipTemplate: "<%if (label){%><%=label%>: <%}%><%= value %>",

								// String - Template string for single tooltips
								multiTooltipTemplate: "<%= value %>",

								// Boolean - If we should show the scale at all
								showScale: true,

								// Boolean - If we want to override with a hard coded scale
								scaleOverride: false,

								// ** Required if scaleOverride is true **
								// Number - The number of steps in a hard coded scale
								//scaleSteps: this.getMaxOfArray(this.faultTimelineData.map(i => i.faultCount)),
								// Number - The value jump in the hard coded scale
								//scaleStepWidth: max,
								// Number - The scale starting value
								scaleStartValue: 0,

								///Boolean - Whether grid lines are shown across the chart
								scaleShowGridLines: true,

								//String - Colour of the grid lines
								scaleGridLineColor: "rgba(0,0,0,.05)",

								//Number - Width of the grid lines
								scaleGridLineWidth: 1,

								//Boolean - Whether the line is curved between points
								bezierCurve: true,

								//Number - Tension of the bezier curve between points
								bezierCurveTension: 0.4,

								//Boolean - Whether to show a dot for each point
								pointDot: true,

								//Number - Radius of each point dot in pixels
								pointDotRadius: 4,

								//Number - Pixel width of point dot stroke
								pointDotStrokeWidth: 1,

								//Number - amount extra to add to the radius to cater for hit detection outside the drawn point
								pointHitDetectionRadius: 20,

								//Boolean - Whether to show a stroke for datasets
								datasetStroke: true,

								//Number - Pixel width of dataset stroke
								datasetStrokeWidth: 2,

								//Boolean - Whether to fill the dataset with a colour
								datasetFill: true,

								//String - A legend template
								legendTemplate: "<ul class=\"<%=name.toLowerCase()%>-legend\"><% for (var i=0; i<datasets.length; i++){%><li><span style=\"background-color:<%=datasets[i].lineColor%>\"></span><%if(datasets[i].label){%><%=datasets[i].label%><%}%></li><%}%></ul>"
							};


						});
						break;
					case "mutators":
						this.peachSvc.GetMutatorMetrics(jobUrl, (data: Models.IMutatorMetric[]) => {
							this.mutatorData = data;
						});
						break;
					case "elements":
						this.peachSvc.GetElementMetrics(jobUrl, (data: Models.IElementMetric[]) => {
							this.elementData = data;
						});
						break;
					case "datasets":
						this.peachSvc.GetDatasetMetrics(jobUrl, (data: Models.IDatasetMetric[]) => {
							this.datasetData = data;
						});
						break;
					case "states":
						this.peachSvc.GetStateMetrics(jobUrl, (data: Models.IStateMetric[]) => {
							this.stateData = data;
						});
						break;
					case "buckets":
						this.peachSvc.GetBucketMetrics(jobUrl, (data: Models.IBucketMetric[]) => {
							this.bucketData = data;
						});
						break;
				}
			});
		}
	}
}
*/
