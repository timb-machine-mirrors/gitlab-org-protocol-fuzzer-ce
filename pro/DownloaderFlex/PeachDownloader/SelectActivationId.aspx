<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SelectActivationId.aspx.cs" Inherits="PeachDownloader.SelectActivationId" %>
<%@ Import Namespace="PeachDownloader" %>
<%

// If license has not been validated, send to validate page
if (!(bool)Session[SessionKeys.Authenticated])
{
    Response.Redirect("Default.aspx", true);
}

if (!string.IsNullOrEmpty(Request["a"]))
{
	Session[SessionKeys.ActivationId] = Request["a"];
	Response.Redirect(string.Format("License.aspx?p={0}&b={1}&f={2}",
		Server.UrlEncode(Request["p"]),
		Server.UrlEncode(Request["b"]),
		Server.UrlEncode(Request["f"])));
}

Session[SessionKeys.ActivationId] = string.Empty;

%>
<!DOCTYPE html>
<%="<!--[if lt IE 7]><html class=\"no-js lt-ie9 lt-ie8 lt-ie7\"><![endif]-->"%>
<%="<!--[if IE 7]><html class=\"no-js lt-ie9 lt-ie8\"><![endif]-->"%>
<%="<!--[if IE 8]><html class=\"no-js lt-ie9\"><![endif]-->"%>
<%="<!--[if gt IE 8]><!--><html class=\"no-js\"><!--<![endif]-->"%>
<head>
	<meta charset="utf-8">
	<meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1">
	<title>
		Peach Customer Portal
	</title>
	<meta name="description" content="">
	<meta name="viewport" content="width=device-width">
	
	<script src="assets/javascripts/1.3.0/adminflare-demo-init.min.js" type="text/javascript"></script>

	<link href="https://fonts.googleapis.com/css?family=Open+Sans:300italic,400italic,600italic,700italic,400,300,600,700" rel="stylesheet" type="text/css">
    <link href="assets/css/1.3.0/default/bootstrap.min.css" media="all" rel="stylesheet" type="text/css" id="bootstrap-css">
    <link href="assets/css/1.3.0/default/adminflare.min.css" media="all" rel="stylesheet" type="text/css" id="adminflare-css">
	
	<script src="assets/javascripts/1.3.0/modernizr-jquery.min.js" type="text/javascript"></script>
	<script src="assets/javascripts/1.3.0/bootstrap.min.js" type="text/javascript"></script>
	<script src="assets/javascripts/1.3.0/adminflare.min.js" type="text/javascript"></script>
</head>
<body>

	<!-- Main navigation bar
		================================================== -->
	<header class="navbar navbar-fixed-top" id="main-navbar">
		<div class="navbar-inner">
			<div class="container">
				<a class="logo" href="#"><img width="100px" alt="Peach Fuzzer Professional" src="Images/peach_fuzzer-white.png"></a>

				<a class="btn nav-button collapsed" data-toggle="collapse" data-target=".nav-collapse">
					<span class="icon-reorder"></span>
				</a>

				<div class="nav-collapse collapse">
					<ul class="nav">
						<li class="active"><a href="#">Downloads</a></li>
						<li><a target="_blank" href="https://flex1253-fno.flexnetoperations.com/flexnet/operationsportal/logon.do">Licensing Portal</a></li>
						<!--<li><a href="training.html">Training</a></li>
						<li><a href="support.html">Support</a></li>-->
						<li class="divider-vertical"></li>
						<li><a href="Logout.aspx">Logout</a></li>
					</ul>
				</div>
			</div>
		</div>
	</header>
	<!-- / Main navigation bar -->
	
	<!-- Left navigation panel
		================================================== -->
	<nav id="left-panel">
		<div id="left-panel-content">
			<ul>
				<li class="active">
					<a href="#"><span class="icon-info-sign"></span>Information</a>
				</li>
			</ul>
		</div>
		<div class="icon-caret-down"></div>
		<div class="icon-caret-up"></div>
	</nav>
	<!-- / Left navigation panel -->
	
	<!-- Page content
		================================================== -->
	<section class="container">

		<!-- Content here
			================================================== -->
		<!-- ================================================== -->
		<section class="row-fluid">
		
			<div class="well widget-pie-charts">
				<div class="box no-border non-collapsible">
    <%
	    var operations = (Operations)Session[SessionKeys.Operations];
	    var activations = operations.ActivationIds();
	    Session[SessionKeys.Activations] = activations;

		// Auto select single activation id
	    if (activations != null && activations.Count == 1)
	    {
		    var act = activations[0];
			var url = string.Format("SelectActivationId.aspx?p={0}&b={1}&f={2}&a={3}",
				Server.UrlEncode(Request["p"]),
				Server.UrlEncode(Request["b"]),
				Server.UrlEncode(Request["f"]),
				Server.UrlEncode(act.ActivationId));
			
			Response.Redirect(url);
	    }
		
	    if (activations == null || activations.Count == 0)
	    {
		    %>
			<h3> Error, no activations found for your account/organization. Please contact sales@peachfuzzer.com for assistance. </h3>
			<%
	    }
	    else
	    {
			%>
			
			<h4>Select One of the Following Activations</h4>

			<p>Your download will be pre-configured to use the selected
			activation id.  The activation id and license server URL can be modified after download by editting the
			<i>Peach.exe.license.config</i> file.</p>
			
			<br/>

			<table>
			<tr><th>Organization</th><th>Activation ID</th><th>Product</th></tr>
		    <%
			
		    foreach (var act in activations)
		    {
				var url = string.Format("SelectActivationId.aspx?p={0}&b={1}&f={2}&a={3}",
					Server.UrlEncode(Request["p"]),
					Server.UrlEncode(Request["b"]),
					Server.UrlEncode(Request["f"]),
					Server.UrlEncode(act.ActivationId));
				
				%>
				<tr>
					<td><%= Server.HtmlEncode(act.OrgName) %></td>
					<td><a href="<%= url %>"><%= act.ActivationId %></a></td>
					<td><%= Server.HtmlEncode(act.Product) %></td>
				</tr>
				<%
		    }
			
			%>
	    </table><%
	    }
    %>
				</div>
			</div>

		</section>
		<!-- / Content here -->
		
		<!-- Page footer
			================================================== -->
		<footer id="main-footer">
			Copyright &copy; 2016 <a href="http://www.peachfuzzer.com">Peach Fuzzer, LLC</a>, all rights reserved.
		</footer>
		<!-- / Page footer -->
	</section>
</body>
</html>
