/// <reference path="../../../Scripts/typings/moment/moment.d.ts" />
/// <reference path="../Services/peach.ts" />
/// <reference path="../Models/models.ts" />

module DashApp {
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
		data?: Models.BucketTimelineMetric
	}

	export class MetricsController {
		private scope: ViewModelScope;
		private debug = true;
		private peachSvc: Services.IPeachService;
		private pitConfigSvc: Services.IPitConfiguratorService;
		private visDataSet;

		public metric: string;

		//public bucketTimelineData: Models.BucketTimelineMetric[] = [];
		public bucketTimelineData = {};

		public mutatorData: Models.MutatorMetric[] = [];
		public elementData: Models.ElementMetric[] = [];
		public datasetData: Models.DatasetMetric[] = [];
		public stateData: Models.StateMetric[] = [];
		public bucketData: Models.BucketMetric[] = [];

		private jobStartDate: Date = new Date();

		static $inject = ["$scope", "$routeParams", "pitConfiguratorService", "peachService", "visDataSet"];

		constructor($scope: ViewModelScope, $routeParams: MetricsParams, pitConfiguratorService: Services.IPitConfiguratorService, peachService: Services.IPeachService, visDataSet) {
			$scope.vm = this;
			this.scope = $scope;
			this.metric = $routeParams.metric;
			this.pitConfigSvc = pitConfiguratorService;
			this.peachSvc = peachService;
			//this.jobStartDate = this.pitConfigSvc.Job.startDate; 
			this.visDataSet = visDataSet;
			this.initializeData();


			this.testData = this.sampleData();
			//this.testOptions = angular.extend(options, {});
		}
		

		private initializeData() {
			this.peachSvc.GetJob((data: Models.Job) => {
				var jobUrl = data.jobUrl;

				switch (this.metric) {
					case "bucketTimeline":
						this.peachSvc.GetBucketTimeline(jobUrl, (data: Models.BucketTimelineMetric[]) => {
							var timelineData = [];
							data.forEach((item) => {
								timelineData.push({
									id: item.id,
									type: "point",
									content: "",
									data: item,
									start: item.time
								});
							});
							var dataset = this.visDataSet(timelineData);
							this.bucketTimelineData = dataset;
							//this.bucketTimelineData = timelineData;
						});
						break;
					case "faultsOverTime":
						this.peachSvc.GetFaultTimeline(jobUrl, (data: Models.FaultTimelineMetric[]) => {
							this.faultTimelineData = data;

							this.metrics_faultsOverTime_chart = {
								labels: this.faultTimelineData.map(i => moment(i.date).format("M/D h a")),
								datasets: [
									{
										label: "My First dataset",
										fillColor: "rgba(220,220,220,0.2)",
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
						this.peachSvc.GetMutatorMetrics(jobUrl, (data: Models.MutatorMetric[]) => {
							this.mutatorData = data;
						});
						break;
					case "elements":
						this.peachSvc.GetElementMetrics(jobUrl, (data: Models.ElementMetric[]) => {
							this.elementData = data;
						});
						break;
					case "datasets":
						this.peachSvc.GetDatasetMetrics(jobUrl, (data: Models.DatasetMetric[]) => {
							this.datasetData = data;
						});
						break;
					case "states":
						this.peachSvc.GetStateMetrics(jobUrl, (data: Models.StateMetric[]) => {
							this.stateData = data;
						});
						break;
					case "buckets":
						this.peachSvc.GetBucketMetrics(jobUrl, (data: Models.BucketMetric[]) => {
							this.bucketData = data;
						});
						break;
				} 
			});


		}

		private simplifyItems(items) {
			var simplified = [];

			angular.forEach(items, function (group, label) {
				angular.forEach(group, function (item) {
					item.group = label;

					simplified.push(item);
				});
			});

			return simplified;
		}


		// #region test timeline
	public testOptions = {
		align: 'center', // left | right (String)
		autoResize: true, // false (Boolean)
		editable: true,
		selectable: true,
		// start: null,
		// end: null,
		// height: null,
		// width: '100%',
		// margin: {
		//   axis: 20,
		//   item: 10
		// },
		// min: null,
		// max: null,
		// maxHeight: null,
		orientation: 'bottom',
		// padding: 5,
		showCurrentTime: true,
		showCustomTime: true,
		showMajorLabels: true,
		showMinorLabels: true
		// type: 'box', // dot | point
		// zoomMin: 1000,
		// zoomMax: 1000 * 60 * 60 * 24 * 30 * 12 * 10,
		// groupOrder: 'content'
	};

	private sampleData = function () {
		return this.visDataSet([
			{
				id: 1,
				content: '<i class="fi-flag"></i> item 1',
				start: moment().add('days', 1),
				className: 'magenta'
			},
			{
				id: 2,
				content: '<a href="http://visjs.org" target="_blank">visjs.org</a>',
				start: moment().add('days', 2)
			},
			{
				id: 3,
				content: 'item 3',
				start: moment().add('days', -2)
			},
			{
				id: 4,
				content: 'item 4',
				start: moment().add('days', 1),
				end: moment().add('days', 3),
				type: 'range'
			},
			{
				id: 7,
				content: '<i class="fi-anchor"></i> item 7',
				start: moment().add('days', -3),
				end: moment().add('days', -2),
				type: 'range',
				className: 'orange'
			},
			{
				id: 5,
				content: 'item 5',
				start: moment().add('days', -1),
				type: 'point'
			},
			{
				id: 6,
				content: 'item 6',
				start: moment().add('days', 4),
				type: 'point'
			}
		]);
	};

		public testData = {};

		// #endregion



		// #region timeline
		public bucketTimelineOptions = {
			debug: false,
			align: 'center',
			autoResize: true,
			editable: false,
			start: null,
			end: null,
			height: null,
			width: '100%',
			margin: {
				axis: 20,
				item: 10
			},
			//min: this.jobStartDate,
			//max: new Date(),
			maxHeight: null,
			orientation: 'top',
			padding: 5,
			selectable: false,
			showCurrentTime: true,
			showCustomTime: true,
			showMajorLabels: true,
			showMinorLabels: true,
			type: 'box', // dot | point
			zoomMin: 1000,
			zoomMax: 1000 * 60 * 60 * 24 * 30 * 12 * 10,
			groupOrder: 'content',
			template: function (item) {
				return "<a ng-click=\"$event.stopPropagation()\" href=\"#/faults/" + item.data.label + "\" ><b>" + item.data.label + "</b></a><br />" +
					"Faults: " + item.data.faultCount + "<br />" +
					"1st Iteration: " + item.data.iteration + "<br />"; 
			}
		};

		public bucketTimelineEvents = {
			//select: (selected) => {
			//	if (this.debug) {
			//		console.log('selected items: ', selected.items);
			//	}

			//	var selecteditem = $.grep(this.bucketTimelineData, (e) => {
			//		return e.id == selected.items[0];
			//	})[0];

			//	var items = this.simplifyItems(this.bucketTimelineData);

			//	var format = 'YYYY-MM-DDTHH:mm'; 

			//	angular.forEach(items, function (item) {
			//		if (item.id == selected.items[0]) {
			//			this.scope.slot = {
			//				id: item.id,
			//				start: moment(item.start).format(format),
			//				end: (item.end) ? moment(item.end).format(format) : null,
			//				content: item.content
			//			};

			//			this.scope.$apply();
			//		}
			//	});
			//},

			range: {},

			rangeChange: (period) => {
				this.bucketTimelineEvents.range = this.scope.vm.timeline.getWindow();

				if (!this.scope.$$phase) {
					this.scope.$apply();
				}

				if (this.debug) {
					console.log('rangeChange: start-> ', period.start, ' end-> ', period.end);
				}
			},

			rangeChanged: function (period) {
				if (this.debug) {
					console.log('rangeChange(d): start-> ', period.start, ' end-> ', period.end);
				}
			},

			customTime: null,

			timeChange: (period) => {
				if (this.debug) {
					console.log('timeChange: ', period.time);
				}

				this.scope.$apply(
					function () {
						this.scope.vm.timeline.customTime = period.time;
					}
					);
			},

			timeChanged: (period) => {
				if (this.debug) {
					console.log('timeChange(d): ', period.time);
				}
			},

			slot: {
				add: (item, callback) => {
					item.content = prompt('Enter text content for new item:', item.content);

					if (item.content != null) {
						callback(item); // send back adjusted new item
					}
					else {
						callback(null); // cancel item creation
					}
				},

				move: (item, callback) => {
					if (confirm(
						'Do you really want to move the item to\n' +
						'start: ' + item.start + '\n' +
						'end: ' + item.end + '?')) {
						callback(item); // send back item as confirmation (can be changed
					}
					else {
						callback(null); // cancel editing item
					}
				},

				update: (item, callback) => {
					item.content = prompt('Edit items text:', item.content);

					if (item.content != null) {
						callback(item); // send back adjusted item
					}
					else {
						callback(null); // cancel updating the item
					}
				},
				remove: (item, callback) => {
					//	if (confirm('Remove item ' + item.content + '?')) {
					//		callback(item); // confirm deletion
					//	}
					//	else {
					//		callback(null); // cancel deletion
					//	}
				}
			}
		};

		//#endregion

		private faultTimelineData: Models.FaultTimelineMetric[] = [];

		//labels: this.faultTimelineData.map(i => moment(i.date).format("h:mm:ss a")),

		public metrics_faultsOverTime_chart = {};

		// #region metrics_faultsOverTime_options

		public metrics_faultsOverTime_options = {};
		//#endregion


		public gridMetricsMutator = {
			data: "vm.mutatorData",
			sortInfo: { fields: ["mutator"], directions: ["asc"] },
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
			sortInfo: { fields: ["element"], directions: ["asc"] },
			columnDefs: [
				{ field: "state", displayName: "State" },
				{ field: "action", displayName: "Action" },
				{ field: "parameter", displayName: "Parameter" },
				{ field: "element", displayName: "Element" },
				{ field: "iterationCount", displayName: "Iterations" },
				{ field: "mutationCount", displayName: "Mutations" },
				{ field: "bucketCount", displayName: "Buckets" },
				{ field: "faultCount", displayName: "Faults" }
			]
		};

		public gridMetricsState = {
			data: "vm.stateData",
			sortInfo: { fields: ["state"], directions: ["asc"] },
			columnDefs: [
				{ field: "state", displayName: "State" },
				{ field: "executionCount", displayName: "Executions" }
			]
		};

		public gridMetricsDataset = {
			data: "vm.datasetData",
			sortInfo: { fields: ["dataset"], directions: ["asc"] },
			columnDefs: [
				{ field: "dataset", displayName: "Data Set" },
				{ field: "iterationCount", displayName: "Iterations" },
				{ field: "bucketCount", displayName: "Buckets" },
				{ field: "faultCount", displayName: "Faults" }
			]
		};

		public gridMetricsBucket = {
			data: "vm.bucketData",
			sortInfo: { fields: ["bucket"], directions: ["asc"] },
			columnDefs: [
				{ field: "bucket", displayName: "Fault Bucket" },
				{ field: "mutator", displayName: "Mutator" },
				{ field: "state", displayName: "State" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			]
		};

		/*
		 		{ field: "dataset", displayName: "Data Set" },
				{ field: "action", displayName: "Action" },
				{ field: "element", displayName: "Element" },

		 */

		private getMaxOfArray(numArray: number[]): number {
			return Math.max.apply(null, numArray);
		}
	}
}