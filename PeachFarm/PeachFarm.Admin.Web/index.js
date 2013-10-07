String.format = function () {
	var s = arguments[0];
	for (var i = 0; i < arguments.length - 1; i++) {
		var reg = new RegExp("\\{" + i + "\\}", "gm");
		s = s.replace(reg, arguments[i + 1]);
	}

	return s;
}

function nodes_fill() {
	var ds = new kendo.data.DataSource({
		transport: {
			read: {
				url: "http://localhost:1337/nodes?format=json",
				dataType: "jsonp",
				data: {

				}

			}
		},
		schema: {
			data: "Nodes"
		}
	});

	$("#nodes").kendoGrid({
		columns: ["Status", "NodeName", "Tags", "Stamp"],
		dataSource: ds
	});



}

//		schema: {data: "InactiveJobs"}, 

function jobs_fill() {
	var ds = new kendo.data.DataSource({
		transport: {
			read: {
				url: "http://localhost:1337/jobs?format=json",
				dataType: "jsonp",
				data: {

				}
			}
		},
		schema: { data: "Jobs" }
		/*
		,
		pageSize: 3,
		serverPaging: true
		*/
	});

	$("#jobs").kendoGrid({
		columns: ["JobID", "Pit.FileName", "StartDate"]
		,dataSource: ds
		//,pageable: true
	});
}