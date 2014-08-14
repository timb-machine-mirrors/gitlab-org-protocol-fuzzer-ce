/// <reference path="../../../Scripts/typings/moment/moment.d.ts" />
/// <reference path="../Services/peach.ts" />
/// <reference path="../Models/models.ts" />

module DashApp {
	"use strict"; 

	export interface MetricsParams extends ng.route.IRouteParamsService {
		metric: string;
	}

	export class MetricsController {
		private scope: ViewModelScope;

		static $inject = ["$scope", "$routeParams"];

		constructor($scope: ViewModelScope, $routeParams: MetricsParams) {
			$scope.vm = this;
			this.scope = $scope;
			this.metric = $routeParams.metric;
		}
		private debug = true;

		public metric: string;

		public bucketTimelineData = [
			{id: 1, content: 'bucket 1', start: '8/12/2014 0:00', url: "/p/jobs/blah"},
			{ id: 2, content: 'bucket 2', start: '8/12/2014 1:00', url: "/p/jobs/blah"},
			{ id: 3, content: 'bucket 3', start: '8/12/2014 2:00', url: "/p/jobs/blah"},
			{ id: 4, content: 'bucket 4', start: '8/12/2014 3:00', url: "/p/jobs/blah"},
			{ id: 5, content: 'bucket 5', start: '8/12/2014 4:00', url: "/p/jobs/blah"},
			{ id: 6, content: 'bucket 6', start: '8/12/2014 5:00', url: "/p/jobs/blah"}
		];

		private simplifyItems (items) {
			var simplified = [];

			angular.forEach(items, function (group, label) {
				angular.forEach(group, function (item) {
					item.group = label;

					simplified.push(item);
				});
			});

			return simplified;
		}

		public timeline = {
			select: (selected) => {
				if (this.debug) {
					console.log('selected items: ', selected.items);
				}

				var selecteditem = $.grep(this.bucketTimelineData, (e) => {
					return e.id == selected.items[0];
				})[0];
				
				var items = this.simplifyItems(this.bucketTimelineData);

				var format = 'YYYY-MM-DDTHH:mm';

				angular.forEach(items, function (item) {
					if (item.id == selected.items[0]) {
						this.scope.slot = {
							id: item.id,
							start: moment(item.start).format(format),
							end: (item.end) ? moment(item.end).format(format) : null,
							content: item.content
						};

						this.scope.$apply();
					}
				});
			},

			range: {},

			rangeChange: (period) => {
				this.timeline.range = this.scope.vm.timeline.getWindow();

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
						this.scope.timeline.customTime = period.time;
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


		public metrics_faultsOverTime_data: Models.FaultTimelineMetric[] = [
			{ time: new Date(2014, 4, 5, 1, 1, 0, 1), faultCount: 1 },
			{ time: new Date(2014, 4, 5, 2, 1, 0, 1), faultCount: 2 },
			{ time: new Date(2014, 4, 5, 3, 1, 0, 1), faultCount: 1 },
			{ time: new Date(2014, 4, 5, 4, 1, 0, 1), faultCount: 3 },
			{ time: new Date(2014, 4, 5, 5, 1, 0, 1), faultCount: 1 },
			{ time: new Date(2014, 4, 5, 6, 1, 0, 1), faultCount: 1 }
		];


		// this.metrics_faultsOverTime_data.map(i => i.x)
		// this.metrics_faultsOverTime_data.map(i => i.y)
		public metrics_faultsOverTime_chart = {
			labels: this.metrics_faultsOverTime_data.map(i => moment(i.time).format("h:mm:ss a")),
			datasets: [
				{
					label: "My First dataset",
					fillColor: "rgba(220,220,220,0.2)",
					strokeColor: "rgba(220,220,220,1)",
					pointColor: "rgba(220,220,220,1)",
					pointStrokeColor: "#fff",
					pointHighlightFill: "#fff",
					pointHighlightStroke: "rgba(220,220,220,1)",
					data: this.metrics_faultsOverTime_data.map(i => i.faultCount)
				}
			]
		};

		public metrics_faultsOverTime_options = { 
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
			scaleOverride: true,

			// ** Required if scaleOverride is true **
			// Number - The number of steps in a hard coded scale
			scaleSteps: this.getMaxOfArray(this.metrics_faultsOverTime_data.map(i => i.faultCount)),
			// Number - The value jump in the hard coded scale
			scaleStepWidth: 1,
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

		public dataMetricsMutator: Models.MutatorMetric[] = [
			{
				mutator: "StringMutator",
				elementCount: 10,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				mutator: "StringMutator",
				elementCount: 10,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				mutator: "StringMutator",
				elementCount: 10,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				mutator: "StringMutator",
				elementCount: 10,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				mutator: "StringMutator",
				elementCount: 10,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
		];

		public gridMetricsMutator = {
			data: "vm.dataMetricsMutator",
			sortInfo: { fields: ["mutator"], directions: ["asc"] },
			columnDefs: [
				{ field: "mutator", displayName: "Mutator" },
				{ field: "elementCount", displayName: "Element Count" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "bucketCount", displayName: "Bucket Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			]
		};

		public dataMetricsElement: Models.ElementMetric[] = [
			{
				dataset: "dataset",
				state: "state",
				action: "action",
				element: "element1",
				mutationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				dataset: "dataset",
				state: "state",
				action: "action",
				element: "element1",
				mutationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				dataset: "dataset",
				state: "state",
				action: "action",
				element: "element1",
				mutationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				dataset: "dataset",
				state: "state",
				action: "action",
				element: "element1",
				mutationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			}
		];

		public gridMetricsElement = {
			data: "vm.dataMetricsElement",
			sortInfo: { fields: ["element"], directions: ["asc"] },
			columnDefs: [
				{ field: "element", displayName: "Element" },
				{ field: "dataset", displayName: "Dataset" },
				{ field: "state", displayName: "State" },
				{ field: "action", displayName: "Action" },
				{ field: "mutatorCount", displayName: "Mutator Count" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "bucketCount", displayName: "Bucket Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			]
		};

		public dataMetricsState: Models.StateMetric[] = [
			{
				state: "State 1",
				executionCount: 5000
			},
			{
				state: "State 2",
				executionCount: 5000
			},
			{
				state: "State 3",
				executionCount: 5000
			},
			{
				state: "State 4",
				executionCount: 5000
			},
			{
				state: "State 5",
				executionCount: 5000
			},
		];

		public gridMetricsState = {
			data: "vm.dataMetricsState",
			sortInfo: { fields: ["state"], directions: ["asc"] },
			columnDefs: [
				{ field: "state", displayName: "State" },
				{ field: "executionCount", displayName: "Execution Count" }
			]
		};

		public dataMetricsData: Models.DatasetMetric[] = [
			{
				dataset: "Dataset",
				element: "FooData",
				mutatorCount: 3,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				dataset: "Dataset",
				element: "FooData",
				mutatorCount: 3,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				dataset: "Dataset",
				element: "FooData",
				mutatorCount: 3,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				dataset: "Dataset",
				element: "FooData",
				mutatorCount: 3,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			},
			{
				dataset: "Dataset",
				element: "FooData",
				mutatorCount: 3,
				iterationCount: 5000,
				bucketCount: 3,
				faultCount: 200
			}
		];

		public gridMetricsDataset = {
			data: "vm.dataMetricsData",
			sortInfo: { fields: ["dataset"], directions: ["asc"] },
			columnDefs: [
				{ field: "dataset", displayName: "Data Set" },
				{ field: "element", displayName: "Element" },
				{ field: "mutatorCount", displayName: "Mutator Count" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "bucketCount", displayName: "Bucket Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			]
		};

		public dataMetricsBucket: Models.BucketMetric[] = [
			{
				bucket: "Bucket 1",
				mutator: "Mutator",
				dataset: "Dataset",
				action: "Action",
				element: "Element",
				state: "State",
				iterationCount: 5000,
				faultCount: 200
			},
			{
				bucket: "Bucket 2",
				mutator: "Mutator",
				dataset: "Dataset",
				action: "Action",
				element: "Element",
				state: "State",
				iterationCount: 5000,
				faultCount: 200
			},
			{
				bucket: "Bucket 3",
				mutator: "Mutator",
				dataset: "Dataset",
				action: "Action",
				element: "Element",
				state: "State",
				iterationCount: 5000,
				faultCount: 200
			},
			{
				bucket: "Bucket 4",
				mutator: "Mutator",
				dataset: "Dataset",
				action: "Action",
				element: "Element",
				state: "State",
				iterationCount: 5000,
				faultCount: 200
			},
			{
				bucket: "Bucket 5",
				mutator: "Mutator",
				dataset: "Dataset",
				action: "Action",
				element: "Element",
				state: "State",
				iterationCount: 5000,
				faultCount: 200
			},
		];

		public gridMetricsBucket = {
			data: "vm.dataMetricsBucket",
			sortInfo: { fields: ["bucket"], directions: ["asc"] },
			columnDefs: [
				{ field: "bucket", displayName: "Fault Bucket" },
				{ field: "mutator", displayName: "Mutator" },
				{ field: "dataset", displayName: "Data Set" },
				{ field: "action", displayName: "Action" },
				{ field: "element", displayName: "Element" },
				{ field: "state", displayName: "State" },
				{ field: "iterationCount", displayName: "Iteration Count" },
				{ field: "faultCount", displayName: "Fault Count" }
			]
		};

		private getMaxOfArray(numArray: number[]) {
			return Math.max.apply(null, numArray);
		}

	}
}