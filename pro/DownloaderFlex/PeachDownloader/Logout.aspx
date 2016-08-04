<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Logout.aspx.cs" Inherits="PeachDownloader.Logout" %>
<%
    Session["LicenseValidation"] = false;
    Response.Redirect("Default.aspx", true);
%>