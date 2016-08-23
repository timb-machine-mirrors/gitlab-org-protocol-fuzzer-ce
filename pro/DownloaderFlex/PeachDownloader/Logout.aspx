<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Logout.aspx.cs" Inherits="PeachDownloader.Logout" %>
<%@ Import Namespace="PeachDownloader" %>
<%
	Session[SessionKeys.Authenticated] = false;
	Session[SessionKeys.Operations] = null;
	Session.Abandon();
	
	Response.Redirect("Default.aspx", true);
%>