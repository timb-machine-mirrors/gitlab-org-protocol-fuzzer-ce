var storedJson =  null;

window.onload = function()
{
  window.addEventListener('resize', function(event){
    updateWidth();
  });

  getJson();
};

function updateWidth(){
  if (storedJson != null)
  {
    parseD3Json(storedJson, true);
  }
}

function getJson() {
  var xmlhttp = new XMLHttpRequest();

  xmlhttp.onreadystatechange = function()
  {
    if (xmlhttp.readyState == 4 && xmlhttp.status == 200)
    {
      var json = xmlhttp.responseText;

      json = JSON.parse(json);
      if(xmlhttp.responseText != "")
        parseD3Json(json, false);
    }
  }

  xmlhttp.open("GET", "http://localhost:8888/p/jobs/1/visualizer", true);
  xmlhttp.send();
}

//Load Json
function parseD3Json(root, redraw)
{
  if(redraw)
    $("#viszContainer").html("");

  storedJson = root;
  //return;
  var width = $("#viszContainer").width();

  var height = $("#viszContainer").height();;

  var nodeTitleXAdjust = 8;
    //create a cluster layout
  var cluster = d3.layout.cluster()
    .size([height - 15, width - 90]);

    //update
  var diagonal = d3.svg.diagonal()
  .projection(function(d) { return [d.y, d.x]; });

  //use # to select by id name
var svg = d3.select("#viszContainer").append("svg:svg")
  .attr("width", width)
  .attr("height", "100%")
  .attr("preserveAspectRatio", "none")
  .append("g")
  .attr({"transform": "translate(30,5)"});

  //Select the First dataModel out of the datamodel secion
  var iteration = 0;
  var iterationTotal = 0;
  var mutatedElement = "";
  var mutationType = "";

  iteration = cluster.nodes(root[0]['IterationNumber']);
  iterationTotal = cluster.nodes(root[0]['TotalIteration']);

  $("#currentIteration").html(iteration);

  fuzzedElements = cluster.nodes(root[1]['MutatedElements']);
  fuzzedElementsLength = cluster.nodes(root[1]['MutatedElements'].length);

  $("#numberOfMutations").html(fuzzedElementsLength);

  //d3.select("#IterationValue").text(iteration);
  //d3.select("#TotalIterValue").text(iterationTotal);

  var fuzzedDataModel = atob(root[2]['DataModels'][0]["FuzzedDataModel"]);
  var originalDataModel = atob(root[2]['DataModels'][1]["OriginalDataModel"]);

  if (!redraw)
    displayDifferences(originalDataModel, fuzzedDataModel)

  var topLevelName = root[2]['DataModels'][2].name;
  var nodes = cluster.nodes(root[2]['DataModels'][2]);
  var links = cluster.links(nodes);

  var link = svg.selectAll(".link")
    .data(links)
    .enter()
    .append("path")
    .attr("class", "link")
    .attr("d", diagonal);

  var node = svg.selectAll(".node")
    .data(nodes)
    .enter()
    .append("g")
    .attr("class", "node")
    .attr("id", function(d)
     {
       var name = d.name;
       currentNode = d;
       var n = d;
       if(n.parent != null)
       {
        while(n!= null && n.parent.name != topLevelName )
        {
          name = n.parent.name +  name;
          if(n.parent != null)
          {
            n = n.parent;
          }
          else
          {
            n = null;
          }
        }
      }

      if(name != topLevelName)
        name = topLevelName + name;
      return name;
    })
    .attr("transform", function(d) { return "translate(" + d.y + "," + d.x + ")"; })


   //var highlight = svg.select("#")

  node.append("circle")
    .attr("r", 6.5);

  node.append("text")
      .attr("dx", function(d) { return d.children ? -nodeTitleXAdjust : nodeTitleXAdjust; })
      .attr("dy", 0)
      .style("text-anchor", function(d) { return d.children ? "end" : "start"; })
      .text(function(d) { return d.name; });

  for(var i = 0; i < fuzzedElementsLength; i++)
  {
    var highlightNodes = fuzzedElements[0][i].split('.');
    var nodeName = "";
    for(var nodeIndex = 0; nodeIndex < highlightNodes.length; nodeIndex++)
    {
      nodeName += highlightNodes[nodeIndex];

      if(highlightNodes.length == nodeIndex + 1)
      {
        d3.select("#" + nodeName ).attr("class", "nodeFuzzed");
        d3.select("#" + nodeName).append("circle").attr("r", 12);
        d3.select("#" + nodeName).classed({"nodeHighlight" : false})
      }
      else if(!d3.select("#" + nodeName).classed("nodeFuzzed"))
      {
        d3.select("#" + nodeName ).attr("class", "nodeHighlight");
        d3.select("#" + nodeName).append("circle").attr("r", 8);
      }
    }
  }

  d3.select(self.frameElement).style("height", height + "px");
  d3.select(self.frameElement).style("width", width + "px");
}