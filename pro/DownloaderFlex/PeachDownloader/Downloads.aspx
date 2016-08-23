<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Downloads.aspx.cs" Inherits="PeachDownloader.Downloads" %>
<%@ Import namespace="System.Linq" %>
<%@ Import Namespace="PeachDownloader" %>
<%
    // If license has not been validated, send to validate page
    if (!(bool)Session[SessionKeys.Authenticated])
    {
        Response.Redirect("Default.aspx", true);
    }

    var selectedVersion = Request.QueryString["v"];
    
%>
<!DOCTYPE html>
<%="<!--[if lt IE 7]><html class=\"no-js lt-ie9 lt-ie8 lt-ie7\"><![endif]-->"%>
<%="<!--[if IE 7]><html class=\"no-js lt-ie9 lt-ie8\"><![endif]-->"%>
<%="<!--[if IE 8]><html class=\"no-js lt-ie9\"><![endif]-->"%>
<%="<!--[if gt IE 8]><!--><html class=\"no-js\"><!--<![endif]-->"%>
<head>
	<meta http-equiv="Content-Language" content="en">
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
<!--						<li><a href="main.html">Home</a></li> -->
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
				<%
					foreach (var version in GetReleases().Reverse())
					{
						if (selectedVersion == null)
							selectedVersion = version;

						if (version == selectedVersion)
						{
							%><li class="active"><%
						}
						else
						{
							%><li><%
						}
						%>

					<a href="Downloads.aspx?v=<%=version %>"><span class="icon-circle-blank"></span><%=version %></a>
				</li>
						<%
						
					} 
				%>
			</ul>
		</div>
		<div class="icon-caret-down"></div>
		<div class="icon-caret-up"></div>
	</nav>
	<!-- / Left navigation panel -->
	
	<section class="container">
		<div class="row-fluid">
			
			<div class="span6">
			
			<%
				foreach (var product in _downloads.Keys)
				{
					foreach (var build in _downloads[product].Keys.Where(k => k.ToString().StartsWith(selectedVersion ?? string.Empty)))
					{
						var release = _downloads[product][build];

						if (release.Version < 2)
							continue;
						
			%>

				<div class="row-fluid">
					<div class="span12">
						<h3 class="box-header">
							 <% if (release.nightly){ %>Nightly<% } else {%>Stable<%} %> v<%=build %> (<%=release.date %>)
						</h3>
						<div class="box" style="padding-bottom: 10px">
							<table class="table table-hover">
								<thead>
									<tr><th>Download</th></tr>
								</thead>
								<tbody>
								<%
							foreach (var file in release.files)
							{

						%>                                                              
									<tr>
										<td><%=file %></td>
										<td></td>
										<td><%=FileSize(release.basePath, file) %>MB</td>
										<td>
											<a href="SelectActivationId.aspx?p=<%= Server.UrlEncode(product)%>&b=<%= Server.UrlEncode(build.ToString())%>&f=<%= Server.UrlEncode(file)%>" alt="Download"><span class="icon-download"></span></a>
										</td>
									</tr>
						<%
							}
						%>
								</tbody>
							</table>
						</div>
					</div>
				</div>
				
			<%
						}
					}
			%>


				<!-- ================================================== -->

			</div>
		</div>
		<!-- / Content here -->
		
		<!-- Page footer
			================================================== -->
		<footer id="main-footer">
			Copyright &copy; 2014 <a href="http://www.peachfuzzer.com">Peach Fuzzer, LLC</a>, all rights reserved.
		</footer>
		<!-- / Page footer -->
	</section>
<%
	
	// When redirected back to this page from the license acceptance, allow download.
	
    if ((bool)Session[SessionKeys.AcceptLicense] && Request["p"] != null)
    {
        %><iframe width="1" height="1" src="dl.aspx?p=<%= Server.UrlEncode(Request["p"]) %>&b=<%= Server.UrlEncode(Request["b"]) %>&f=<%= Server.UrlEncode(Request["f"]) %>" /><%
    }
%>
</body>
</html>
